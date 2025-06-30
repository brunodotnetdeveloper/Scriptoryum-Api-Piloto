using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registra um novo usuário no sistema
    /// </summary>
    /// <param name="registerDto">Dados para registro do usuário</param>
    /// <returns>Resposta da autenticação com token JWT</returns>
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Registrar novo usuário",
        Description = "Cria uma nova conta de usuário e retorna um token JWT para autenticação"
    )]
    [SwaggerResponse(200, "Usuário registrado com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Dados inválidos ou usuário já existe", typeof(AuthResponseDto))]
    [SwaggerResponse(500, "Erro interno do servidor", typeof(AuthResponseDto))]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = errors
                });
            }

            var result = await _authService.RegisterAsync(registerDto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Usuário {Email} registrado com sucesso", registerDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar usuário {Email}", registerDto.Email);
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    /// <summary>
    /// Realiza login do usuário
    /// </summary>
    /// <param name="loginDto">Credenciais de login</param>
    /// <returns>Resposta da autenticação com token JWT</returns>
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Fazer login",
        Description = "Autentica o usuário e retorna um token JWT para acesso às APIs protegidas"
    )]
    [SwaggerResponse(200, "Login realizado com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Credenciais inválidas", typeof(AuthResponseDto))]
    [SwaggerResponse(401, "Não autorizado", typeof(AuthResponseDto))]
    [SwaggerResponse(500, "Erro interno do servidor", typeof(AuthResponseDto))]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Dados inválidos",
                    Errors = errors
                });
            }

            var result = await _authService.LoginAsync(loginDto);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            _logger.LogInformation("Usuário {Email} fez login com sucesso", loginDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer login do usuário {Email}", loginDto.Email);
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    /// <summary>
    /// Renova o token JWT
    /// </summary>
    /// <param name="token">Token JWT atual (pode estar expirado)</param>
    /// <returns>Novo token JWT</returns>
    [HttpPost("refresh-token")]
    [SwaggerOperation(
        Summary = "Renovar token",
        Description = "Gera um novo token JWT baseado no token atual (mesmo que expirado)"
    )]
    [SwaggerResponse(200, "Token renovado com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Token inválido", typeof(AuthResponseDto))]
    [SwaggerResponse(500, "Erro interno do servidor", typeof(AuthResponseDto))]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Token é obrigatório",
                    Errors = new List<string> { "Token não fornecido" }
                });
            }

            var result = await _authService.RefreshTokenAsync(token);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao renovar token");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = new List<string> { "Ocorreu um erro inesperado" }
            });
        }
    }

    /// <summary>
    /// Obtém informações do usuário autenticado
    /// </summary>
    /// <returns>Informações do usuário atual</returns>
    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Obter informações do usuário",
        Description = "Retorna as informações do usuário autenticado baseado no token JWT"
    )]
    [SwaggerResponse(200, "Informações do usuário", typeof(UserInfoDto))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Usuário não encontrado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var userInfo = await _authService.GetUserInfoAsync(userId);
            
            if (userInfo == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Verifica se o token JWT é válido
    /// </summary>
    /// <returns>Status da validação do token</returns>
    [HttpGet("validate-token")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Validar token",
        Description = "Verifica se o token JWT fornecido é válido e não expirou"
    )]
    [SwaggerResponse(200, "Token válido")]
    [SwaggerResponse(401, "Token inválido ou expirado")]
    public ActionResult ValidateToken()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            return Ok(new
            {
                valid = true,
                message = "Token válido",
                user = new
                {
                    id = userId,
                    userName = userName,
                    email = email
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar token");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}