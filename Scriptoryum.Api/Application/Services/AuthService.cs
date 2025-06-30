using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Scriptoryum.Api.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RefreshTokenAsync(string token);
    Task<UserInfoDto> GetUserInfoAsync(string userId);
    string GenerateJwtToken(ApplicationUser user, IList<string> roles);
}

public class AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // Verificar se o usuário já existe
            var existingUser = await userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Usuário já existe com este email",
                    Errors = new List<string> { "Email já está em uso" }
                };
            }

            // Verificar se o nome de usuário já existe
            var existingUserName = await userManager.FindByNameAsync(registerDto.UserName);
            if (existingUserName != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Nome de usuário já existe",
                    Errors = new List<string> { "Nome de usuário já está em uso" }
                };
            }

            // Criar novo usuário
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                EmailConfirmed = false // Pode ser configurado para true se não usar confirmação por email
            };

            var result = await userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Erro ao criar usuário",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            // Obter roles do usuário (vazio para novo usuário)
            var roles = await userManager.GetRolesAsync(user);

            // Gerar token JWT
            var token = GenerateJwtToken(user, roles);

            var userInfo = new UserInfoDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };

            logger.LogInformation("Usuário {Email} registrado com sucesso", registerDto.Email);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Usuário registrado com sucesso",
                Token = token,
                TokenExpiration = DateTime.UtcNow.AddHours(24),
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao registrar usuário {Email}", registerDto.Email);
            return new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Credenciais inválidas",
                    Errors = new List<string> { "Email ou senha incorretos" }
                };
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                var errorMessage = result.IsLockedOut ? "Conta bloqueada temporariamente" :
                                 result.IsNotAllowed ? "Login não permitido" :
                                 "Email ou senha incorretos";

                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Falha na autenticação",
                    Errors = new List<string> { errorMessage }
                };
            }

            // Obter roles do usuário
            var roles = await userManager.GetRolesAsync(user);

            // Gerar token JWT
            var token = GenerateJwtToken(user, roles);

            var userInfo = new UserInfoDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };

            logger.LogInformation("Usuário {Email} fez login com sucesso", loginDto.Email);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso",
                Token = token,
                TokenExpiration = DateTime.UtcNow.AddHours(24),
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao fazer login do usuário {Email}", loginDto.Email);
            return new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            };
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = false, // Não validar expiração para refresh
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Token inválido",
                    Errors = new List<string> { "Não foi possível extrair informações do usuário do token" }
                };
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Usuário não encontrado",
                    Errors = new List<string> { "Usuário não existe" }
                };
            }

            var roles = await userManager.GetRolesAsync(user);
            var newToken = GenerateJwtToken(user, roles);

            var userInfo = new UserInfoDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };

            return new AuthResponseDto
            {
                Success = true,
                Message = "Token renovado com sucesso",
                Token = newToken,
                TokenExpiration = DateTime.UtcNow.AddHours(24),
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao renovar token");
            return new AuthResponseDto
            {
                Success = false,
                Message = "Erro ao renovar token",
                Errors = ["Token inválido ou expirado"]
            };
        }
    }

    public async Task<UserInfoDto> GetUserInfoAsync(string userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await userManager.GetRolesAsync(user);

            return new UserInfoDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter informações do usuário {UserId}", userId);
            return null;
        }
    }

    public string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Adicionar roles como claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}