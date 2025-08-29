using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[ApiController]
[Route("api/workspace")]
[Authorize]
[Produces("application/json")]
public class WorkspaceController(IWorkspaceService workspaceService, IDocumentsService documentsService, ILogger<WorkspaceController> logger) : ControllerBase
{

    /// <summary>
    /// Obtém todos os workspaces
    /// </summary>
    /// <returns>Lista de workspaces</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Obter todos os workspaces",
        Description = "Retorna a lista de todos os workspaces disponíveis"
    )]
    [SwaggerResponse(200, "Workspaces carregados com sucesso", typeof(IEnumerable<WorkspaceDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<IEnumerable<WorkspaceDto>>> GetAllWorkspaces()
    {
        try
        {
            var workspaces = await workspaceService.GetAllWorkspacesAsync();
            return Ok(workspaces);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter todos os workspaces");
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém um workspace por ID
    /// </summary>
    /// <param name="id">ID do workspace</param>
    /// <returns>Workspace encontrado</returns>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Obter workspace por ID",
        Description = "Retorna um workspace específico pelo seu ID"
    )]
    [SwaggerResponse(200, "Workspace encontrado", typeof(WorkspaceDto))]
    [SwaggerResponse(404, "Workspace não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<WorkspaceDto>> GetWorkspaceById(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Usuário não autenticado" });
            }

            var workspace = await workspaceService.GetWorkspaceByIdAsync(id, userId);
            
            if (workspace == null)
            {
                return NotFound(new { Success = false, Message = "Workspace não encontrado" });
            }

            return Ok(workspace);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter workspace {WorkspaceId}", id);
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Cria um novo workspace
    /// </summary>
    /// <param name="createDto">Dados do workspace a ser criado</param>
    /// <returns>Workspace criado</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Criar novo workspace",
        Description = "Cria um novo workspace na organização do usuário autenticado"
    )]
    [SwaggerResponse(201, "Workspace criado com sucesso", typeof(WorkspaceDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<WorkspaceDto>> CreateWorkspace([FromBody] CreateWorkspaceDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { Success = false, Message = "Dados inválidos", Errors = errors });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Usuário não autenticado" });
            }

            var workspace = await workspaceService.CreateWorkspaceAsync(createDto, userId);
            
            logger.LogInformation("Workspace {WorkspaceName} criado com sucesso por usuário {UserId}", createDto.Name, userId);
            return CreatedAtAction(nameof(GetWorkspaceById), new { id = workspace.Id }, workspace);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar workspace {WorkspaceName}", createDto.Name);
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualiza um workspace
    /// </summary>
    /// <param name="id">ID do workspace</param>
    /// <param name="updateDto">Dados atualizados do workspace</param>
    /// <returns>Workspace atualizado</returns>
    [HttpPut("{id}")]
    [SwaggerOperation(
        Summary = "Atualizar workspace",
        Description = "Atualiza os dados de um workspace existente"
    )]
    [SwaggerResponse(200, "Workspace atualizado com sucesso", typeof(WorkspaceDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Workspace não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<WorkspaceDto>> UpdateWorkspace(int id, [FromBody] UpdateWorkspaceDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { Success = false, Message = "Dados inválidos", Errors = errors });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Usuário não autenticado" });
            }

            var workspace = await workspaceService.UpdateWorkspaceAsync(id, updateDto, userId);
            
            if (workspace == null)
            {
                return NotFound(new { Success = false, Message = "Workspace não encontrado" });
            }

            logger.LogInformation("Workspace {WorkspaceId} atualizado por usuário {UserId}", id, userId);
            return Ok(workspace);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar workspace {WorkspaceId}", id);
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Deleta um workspace
    /// </summary>
    /// <param name="id">ID do workspace</param>
    /// <returns>Resultado da operação</returns>
    [HttpDelete("{id}")]
    [SwaggerOperation(
        Summary = "Deletar workspace",
        Description = "Deleta um workspace existente (apenas owners)"
    )]
    [SwaggerResponse(204, "Workspace deletado com sucesso")]
    [SwaggerResponse(404, "Workspace não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult> DeleteWorkspace(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Usuário não autenticado" });
            }

            var result = await workspaceService.DeleteWorkspaceAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new { Success = false, Message = "Workspace não encontrado" });
            }

            logger.LogInformation("Workspace {WorkspaceId} deletado por usuário {UserId}", id, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao deletar workspace {WorkspaceId}", id);
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém os workspaces do usuário autenticado
    /// </summary>
    /// <returns>Lista de workspaces do usuário</returns>
    [HttpGet("my-workspaces")]
    [SwaggerOperation(
        Summary = "Obter meus workspaces",
        Description = "Retorna a lista de workspaces aos quais o usuário autenticado tem acesso"
    )]
    [SwaggerResponse(200, "Workspaces do usuário carregados com sucesso", typeof(IEnumerable<WorkspaceDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<IEnumerable<WorkspaceDto>>> GetMyWorkspaces()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Usuário não autenticado" });
            }

            var workspaces = await workspaceService.GetUserWorkspacesAsync(userId);
            return Ok(workspaces);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter workspaces do usuário");
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém os usuários de um workspace
    /// </summary>
    /// <param name="workspaceId">ID do workspace</param>
    /// <returns>Lista de usuários do workspace</returns>
    [HttpGet("{workspaceId}/users")]
    [SwaggerOperation(
        Summary = "Obter usuários do workspace",
        Description = "Retorna a lista de usuários que têm acesso ao workspace"
    )]
    [SwaggerResponse(200, "Usuários do workspace carregados com sucesso", typeof(IEnumerable<WorkspaceUserDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<IEnumerable<WorkspaceUserDto>>> GetWorkspaceUsers(int workspaceId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Usuário não autenticado" });
            }

            var users = await workspaceService.GetWorkspaceUsersAsync(workspaceId, userId);
            return Ok(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter usuários do workspace {WorkspaceId}", workspaceId);
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Adiciona um usuário ao workspace
    /// </summary>
    /// <param name="workspaceId">ID do workspace</param>
    /// <param name="addUserDto">Dados do usuário a ser adicionado</param>
    /// <returns>Usuário adicionado ao workspace</returns>
    [HttpPost("{workspaceId}/users")]
    [SwaggerOperation(
        Summary = "Adicionar usuário ao workspace",
        Description = "Adiciona um usuário ao workspace com o papel especificado"
    )]
    [SwaggerResponse(201, "Usuário adicionado com sucesso", typeof(WorkspaceUserDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<WorkspaceUserDto>> AddUserToWorkspace(int workspaceId, [FromBody] AddUserToWorkspaceDto addUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { Success = false, Message = "Dados inválidos", Errors = errors });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Usuário não autenticado" });
            }

            var workspaceUser = await workspaceService.AddUserToWorkspaceAsync(workspaceId, addUserDto, userId);
            
            if (workspaceUser == null)
            {
                return BadRequest(new { Success = false, Message = "Não foi possível adicionar o usuário ao workspace" });
            }

            logger.LogInformation("Usuário {UserEmail} adicionado ao workspace {WorkspaceId}", addUserDto.UserEmail, workspaceId);
            return CreatedAtAction(nameof(GetWorkspaceUsers), new { workspaceId }, workspaceUser);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao adicionar usuário {UserEmail} ao workspace {WorkspaceId}", addUserDto.UserEmail, workspaceId);
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualiza um usuário do workspace
    /// </summary>
    /// <param name="workspaceId">ID do workspace</param>
    /// <param name="userId">ID do usuário</param>
    /// <param name="updateDto">Dados atualizados do usuário</param>
    /// <returns>Usuário atualizado</returns>
    [HttpPut("{workspaceId}/users/{userId}")]
    [SwaggerOperation(
        Summary = "Atualizar usuário do workspace",
        Description = "Atualiza o papel e status de um usuário no workspace"
    )]
    [SwaggerResponse(200, "Usuário atualizado com sucesso", typeof(WorkspaceUserDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Usuário não encontrado no workspace")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult<WorkspaceUserDto>> UpdateWorkspaceUser(int workspaceId, string userId, [FromBody] UpdateWorkspaceUserDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { Success = false, Message = "Dados inválidos", Errors = errors });
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { Success = false, Message = "Usuário não autenticado" });
            }

            var workspaceUser = await workspaceService.UpdateWorkspaceUserAsync(workspaceId, userId, updateDto, currentUserId);
            
            if (workspaceUser == null)
            {
                return NotFound(new { Success = false, Message = "Usuário não encontrado no workspace" });
            }

            logger.LogInformation("Usuário {UserId} atualizado no workspace {WorkspaceId}", userId, workspaceId);
            return Ok(workspaceUser);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar usuário {UserId} no workspace {WorkspaceId}", userId, workspaceId);
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Remove um usuário do workspace
    /// </summary>
    /// <param name="workspaceId">ID do workspace</param>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Resultado da operação</returns>
    [HttpDelete("{workspaceId}/users/{userId}")]
    [SwaggerOperation(
        Summary = "Remover usuário do workspace",
        Description = "Remove um usuário do workspace"
    )]
    [SwaggerResponse(204, "Usuário removido com sucesso")]
    [SwaggerResponse(404, "Usuário não encontrado no workspace")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult> RemoveUserFromWorkspace(int workspaceId, string userId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { Success = false, Message = "Usuário não autenticado" });
            }

            var result = await workspaceService.RemoveUserFromWorkspaceAsync(workspaceId, userId, currentUserId);
            
            if (!result)
            {
                return NotFound(new { Success = false, Message = "Usuário não encontrado no workspace" });
            }

            logger.LogInformation("Usuário {UserId} removido do workspace {WorkspaceId}", userId, workspaceId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao remover usuário {UserId} do workspace {WorkspaceId}", userId, workspaceId);
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista todos os documentos de um workspace
    /// </summary>
    /// <param name="workspaceId">ID do workspace</param>
    /// <returns>Lista de documentos do workspace</returns>
    [HttpGet("{workspaceId}/documents")]
    [SwaggerOperation(
        Summary = "Listar documentos do workspace",
        Description = "Retorna todos os documentos de um workspace específico"
    )]
    [SwaggerResponse(200, "Documentos carregados com sucesso")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(403, "Acesso negado ao workspace")]
    [SwaggerResponse(500, "Erro interno do servidor")]
    public async Task<ActionResult> GetWorkspaceDocuments(int workspaceId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Token inválido" });
            }

            var documents = await documentsService.GetDocumentsByWorkspaceAsync(workspaceId, userId);

            return Ok(new { documents, count = documents.Count() });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter documentos do workspace {WorkspaceId}", workspaceId);
            return StatusCode(500, new { Success = false, Message = "Erro interno do servidor" });
        }
    }
}