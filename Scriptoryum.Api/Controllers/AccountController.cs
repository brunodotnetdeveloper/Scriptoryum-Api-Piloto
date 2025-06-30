using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Services;
using Scriptoryum.Api.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AccountController(IDocumentsService documentsService, UserManager<ApplicationUser> userManager, ILogger<AccountController> logger) : ControllerBase
{

    /// <summary>
    /// Obtém o perfil do usuário autenticado
    /// </summary>
    /// <returns>Informações do perfil do usuário</returns>
    [HttpGet("profile")]
    [SwaggerOperation(
        Summary = "Obter perfil do usuário",
        Description = "Retorna as informações completas do perfil do usuário autenticado"
    )]
    [SwaggerResponse(200, "Perfil do usuário", typeof(UserInfoDto))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Usuário não encontrado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<UserInfoDto>> GetProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            var roles = await userManager.GetRolesAsync(user);

            var userInfo = new UserInfoDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter perfil do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualiza o perfil do usuário
    /// </summary>
    /// <param name="updateProfileDto">Dados para atualização do perfil</param>
    /// <returns>Resultado da atualização</returns>
    [HttpPut("profile")]
    [SwaggerOperation(
        Summary = "Atualizar perfil",
        Description = "Atualiza as informações do perfil do usuário autenticado"
    )]
    [SwaggerResponse(200, "Perfil atualizado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Usuário não encontrado")]
    [SwaggerResponse(409, "Conflito - Email ou nome de usuário já existe")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { message = "Dados inválidos", errors });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            // Verificar se o email já está em uso por outro usuário
            if (user.Email != updateProfileDto.Email)
            {
                var existingEmailUser = await userManager.FindByEmailAsync(updateProfileDto.Email);
                if (existingEmailUser != null && existingEmailUser.Id != userId)
                {
                    return Conflict(new { message = "Email já está em uso por outro usuário" });
                }
            }

            // Verificar se o nome de usuário já está em uso por outro usuário
            if (user.UserName != updateProfileDto.UserName)
            {
                var existingUserNameUser = await userManager.FindByNameAsync(updateProfileDto.UserName);
                if (existingUserNameUser != null && existingUserNameUser.Id != userId)
                {
                    return Conflict(new { message = "Nome de usuário já está em uso" });
                }
            }

            // Atualizar dados do usuário
            user.UserName = updateProfileDto.UserName;
            user.Email = updateProfileDto.Email;

            // Se o email mudou, marcar como não confirmado
            if (user.Email != updateProfileDto.Email)
            {
                user.EmailConfirmed = false;
            }

            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { message = "Erro ao atualizar perfil", errors });
            }

            logger.LogInformation("Perfil do usuário {UserId} atualizado com sucesso", userId);

            return Ok(new { message = "Perfil atualizado com sucesso" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar perfil do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Altera a senha do usuário
    /// </summary>
    /// <param name="changePasswordDto">Dados para alteração da senha</param>
    /// <returns>Resultado da alteração</returns>
    [HttpPost("change-password")]
    [SwaggerOperation(
        Summary = "Alterar senha",
        Description = "Altera a senha do usuário autenticado"
    )]
    [SwaggerResponse(200, "Senha alterada com sucesso")]
    [SwaggerResponse(400, "Dados inválidos ou senha atual incorreta")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Usuário não encontrado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { message = "Dados inválidos", errors });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            var result = await userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { message = "Erro ao alterar senha", errors });
            }

            logger.LogInformation("Senha do usuário {UserId} alterada com sucesso", userId);

            return Ok(new { message = "Senha alterada com sucesso" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao alterar senha do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Exclui a conta do usuário
    /// </summary>
    /// <param name="password">Senha atual para confirmação</param>
    /// <returns>Resultado da exclusão</returns>
    [HttpDelete("delete-account")]
    [SwaggerOperation(
        Summary = "Excluir conta",
        Description = "Exclui permanentemente a conta do usuário autenticado"
    )]
    [SwaggerResponse(200, "Conta excluída com sucesso")]
    [SwaggerResponse(400, "Senha incorreta")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Usuário não encontrado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult> DeleteAccount([FromBody] string password)
    {
        try
        {
            if (string.IsNullOrEmpty(password))
            {
                return BadRequest(new { message = "Senha é obrigatória para confirmar a exclusão" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            // Verificar senha antes de excluir
            var passwordValid = await userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                return BadRequest(new { message = "Senha incorreta" });
            }

            var result = await userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { message = "Erro ao excluir conta", errors });
            }

            logger.LogInformation("Conta do usuário {UserId} excluída com sucesso", userId);

            return Ok(new { message = "Conta excluída com sucesso" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao excluir conta do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista todos os documentos do usuário
    /// </summary>
    /// <returns>Lista de documentos do usuário</returns>
    [HttpGet("documents")]
    [SwaggerOperation(
        Summary = "Listar documentos do usuário",
        Description = "Retorna todos os documentos associados ao usuário autenticado"
    )]
    [SwaggerResponse(200, "Lista de documentos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Usuário não encontrado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult> GetUserDocuments()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Token inválido" });

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "Usuário não encontrado" });

            var documents = await documentsService.GetDocumentsByUserAsync(userId);

            return Ok(new { documents, count = documents.Count() });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter documentos do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }
}