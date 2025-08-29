using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Services;
using Scriptoryum.Api.Domain.Enums;
using System.Security.Claims;

namespace Scriptoryum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationController(IOrganizationService organizationService, ILogger<OrganizationController> logger) : ControllerBase
{
    private readonly IOrganizationService _organizationService = organizationService;
    private readonly ILogger<OrganizationController> _logger = logger;

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Usuário não autenticado");
    }

    /// <summary>
    /// Obtém todas as organizações (apenas para administradores do sistema)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<IEnumerable<OrganizationDto>>> GetAllOrganizations()
    {
        try
        {
            var organizations = await _organizationService.GetAllOrganizationsAsync();
            return Ok(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todas as organizações");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém uma organização específica por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationDto>> GetOrganizationById(int id)
    {
        try
        {
            var organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                return NotFound("Organização não encontrada");
            }

            return Ok(organization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter organização {OrganizationId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém as organizações do usuário atual
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<OrganizationDto>>> GetMyOrganizations()
    {
        try
        {
            var userId = GetCurrentUserId();
            var organizations = await _organizationService.GetMyOrganizationsAsync(userId);
            return Ok(organizations);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Tentativa de acesso não autorizado");
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter organizações do usuário");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Cria uma nova organização
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> CreateOrganization([FromBody] CreateOrganizationDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var organization = await _organizationService.CreateOrganizationAsync(createDto, userId);
            
            return CreatedAtAction(
                nameof(GetOrganizationById),
                new { id = organization.Id },
                organization);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Tentativa de acesso não autorizado");
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar organização");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Atualiza uma organização existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<OrganizationDto>> UpdateOrganization(int id, [FromBody] UpdateOrganizationDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var organization = await _organizationService.UpdateOrganizationAsync(id, updateDto, userId);
            
            if (organization == null)
            {
                return NotFound("Organização não encontrada");
            }

            return Ok(organization);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Tentativa de acesso não autorizado para atualizar organização {OrganizationId}", id);
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar organização {OrganizationId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Deleta uma organização (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOrganization(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _organizationService.DeleteOrganizationAsync(id, userId);
            
            if (!success)
            {
                return NotFound("Organização não encontrada");
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Tentativa de acesso não autorizado para deletar organização {OrganizationId}", id);
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao deletar organização {OrganizationId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar organização {OrganizationId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém os usuários de uma organização
    /// </summary>
    [HttpGet("{id}/users")]
    public async Task<ActionResult<IEnumerable<OrganizationUserDto>>> GetOrganizationUsers(int id)
    {
        try
        {
            var users = await _organizationService.GetOrganizationUsersAsync(id);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter usuários da organização {OrganizationId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Adiciona um usuário à organização
    /// </summary>
    [HttpPost("{id}/users")]
    public async Task<ActionResult<OrganizationUserDto>> AddUserToOrganization(
        int id, 
        [FromBody] AddUserToOrganizationDto addUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = GetCurrentUserId();
            var organizationUser = await _organizationService.AddUserToOrganizationAsync(
                id, 
                addUserDto.UserEmail, 
                addUserDto.Role, 
                currentUserId);
            
            if (organizationUser == null)
            {
                return BadRequest("Não foi possível adicionar o usuário à organização");
            }

            return CreatedAtAction(
                nameof(GetOrganizationUsers),
                new { id },
                organizationUser);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Tentativa de acesso não autorizado para adicionar usuário à organização {OrganizationId}", id);
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido ao adicionar usuário à organização {OrganizationId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar usuário à organização {OrganizationId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Atualiza um usuário da organização
    /// </summary>
    [HttpPut("{organizationId}/users/{userId}")]
    public async Task<ActionResult<OrganizationUserDto>> UpdateOrganizationUser(
        int organizationId, 
        string userId, 
        [FromBody] UpdateOrganizationUserDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = GetCurrentUserId();
            var organizationUser = await _organizationService.UpdateOrganizationUserAsync(
                organizationId, 
                userId, 
                updateDto.Role, 
                updateDto.Status, 
                currentUserId);
            
            if (organizationUser == null)
            {
                return NotFound("Usuário não encontrado na organização");
            }

            return Ok(organizationUser);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Tentativa de acesso não autorizado para atualizar usuário na organização {OrganizationId}", organizationId);
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao atualizar usuário na organização {OrganizationId}", organizationId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar usuário {UserId} na organização {OrganizationId}", userId, organizationId);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Remove um usuário da organização
    /// </summary>
    [HttpDelete("{organizationId}/users/{userId}")]
    public async Task<ActionResult> RemoveUserFromOrganization(int organizationId, string userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _organizationService.RemoveUserFromOrganizationAsync(
                organizationId, 
                userId, 
                currentUserId);
            
            if (!success)
            {
                return NotFound("Usuário não encontrado na organização");
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Tentativa de acesso não autorizado para remover usuário da organização {OrganizationId}", organizationId);
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao remover usuário da organização {OrganizationId}", organizationId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover usuário {UserId} da organização {OrganizationId}", userId, organizationId);
            return StatusCode(500, "Erro interno do servidor");
        }
    }
}

/// <summary>
/// DTO para adicionar usuário à organização
/// </summary>
public class AddUserToOrganizationDto
{
    public string UserEmail { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; }
}

/// <summary>
/// DTO para atualizar usuário da organização
/// </summary>
public class UpdateOrganizationUserDto
{
    public OrganizationRole Role { get; set; }
    public OrganizationUserStatus Status { get; set; }
}