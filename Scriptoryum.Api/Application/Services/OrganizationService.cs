using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Context;
using System.Security.Claims;

namespace Scriptoryum.Api.Application.Services;

public interface IOrganizationService
{
    Task<IEnumerable<OrganizationDto>> GetAllOrganizationsAsync();
    Task<OrganizationDto?> GetOrganizationByIdAsync(int id);
    Task<OrganizationDto> CreateOrganizationAsync(CreateOrganizationDto createDto, string userId);
    Task<OrganizationDto?> UpdateOrganizationAsync(int id, UpdateOrganizationDto updateDto, string userId);
    Task<bool> DeleteOrganizationAsync(int id, string userId);
    Task<IEnumerable<OrganizationDto>> GetMyOrganizationsAsync(string userId);
    
    // Organization Users
    Task<IEnumerable<OrganizationUserDto>> GetOrganizationUsersAsync(int organizationId);
    Task<OrganizationUserDto?> AddUserToOrganizationAsync(int organizationId, string userEmail, OrganizationRole role, string currentUserId);
    Task<OrganizationUserDto?> UpdateOrganizationUserAsync(int organizationId, string userId, OrganizationRole role, OrganizationUserStatus status, string currentUserId);
    Task<bool> RemoveUserFromOrganizationAsync(int organizationId, string userId, string currentUserId);
}

public class OrganizationService(ScriptoryumDbContext context, ILogger<OrganizationService> logger) : IOrganizationService
{
    private readonly ScriptoryumDbContext _context = context;
    private readonly ILogger<OrganizationService> _logger = logger;

    public async Task<IEnumerable<OrganizationDto>> GetAllOrganizationsAsync()
    {
        try
        {
            var organizations = await _context.Organizations
                .Include(o => o.Users)
                .Include(o => o.Workspaces)
                .Where(o => o.Status != OrganizationStatus.Deleted)
                .ToListAsync();

            return organizations.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todas as organizações");
            throw;
        }
    }

    public async Task<OrganizationDto?> GetOrganizationByIdAsync(int id)
    {
        try
        {
            var organization = await _context.Organizations
                .Include(o => o.Users)
                .Include(o => o.Workspaces)
                .FirstOrDefaultAsync(o => o.Id == id && o.Status != OrganizationStatus.Deleted);

            return organization != null ? MapToDto(organization) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter organização por ID {OrganizationId}", id);
            throw;
        }
    }

    public async Task<OrganizationDto> CreateOrganizationAsync(CreateOrganizationDto createDto, string userId)
    {
        try
        {
            var organization = new Organization
            {
                Name = createDto.Name,
                //Description = createDto.Description,
                ContactEmail = createDto.ContactEmail,
                ContactPhone = createDto.ContactPhone,
                Address = createDto.Address,
                Status = OrganizationStatus.Active
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Atualizar o usuário criador como Owner da organização
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.OrganizationId = organization.Id;
                user.Role = OrganizationRole.Owner;
                user.Status = OrganizationUserStatus.Active;
                user.JoinedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Organização {OrganizationId} criada por usuário {UserId}", organization.Id, userId);
            
            // Recarregar a organização com todas as relações
            var createdOrganization = await _context.Organizations
                .Include(o => o.Users)
                .Include(o => o.Workspaces)
                .FirstAsync(o => o.Id == organization.Id);

            return MapToDto(createdOrganization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar organização");
            throw;
        }
    }

    public async Task<OrganizationDto?> UpdateOrganizationAsync(int id, UpdateOrganizationDto updateDto, string userId)
    {
        try
        {
            var organization = await _context.Organizations
            .Include(o => o.Users)
            .Include(o => o.Workspaces)
            .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                return null;
            }

            // Verificar permissões
            var user = organization.Users
                .FirstOrDefault(u => u.Id == userId && u.Status == OrganizationUserStatus.Active);

            if (user == null || (user.Role != OrganizationRole.Owner && user.Role != OrganizationRole.Admin))
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para atualizar esta organização");
            }

            organization.Name = updateDto.Name;
            //organization.Description = updateDto.Description;
            organization.ContactEmail = updateDto.ContactEmail;
            organization.ContactPhone = updateDto.ContactPhone;
            organization.Address = updateDto.Address;
            
            if (Enum.TryParse<OrganizationStatus>(updateDto.Status, out var status))
            {
                organization.Status = status;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Organização {OrganizationId} atualizada por usuário {UserId}", id, userId);
            return MapToDto(organization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar organização {OrganizationId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteOrganizationAsync(int id, string userId)
    {
        try
        {
            var organization = await _context.Organizations
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                return false;
            }

            // Verificar permissões - apenas Owner pode deletar
            var user = organization.Users
                .FirstOrDefault(u => u.Id == userId && u.Status == OrganizationUserStatus.Active);

            if (user == null || user.Role != OrganizationRole.Owner)
            {
                throw new UnauthorizedAccessException("Apenas o proprietário pode deletar a organização");
            }

            organization.Status = OrganizationStatus.Deleted;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Organização {OrganizationId} deletada por usuário {UserId}", id, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar organização {OrganizationId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<OrganizationDto>> GetMyOrganizationsAsync(string userId)
    {
        try
        {
            var organizations = await _context.Organizations
                .Include(o => o.Users)
                .Include(o => o.Workspaces)
                .Where(o => o.Users.Any(u => u.Id == userId && u.Status == OrganizationUserStatus.Active) &&
                           o.Status != OrganizationStatus.Deleted)
                .ToListAsync();

            return organizations.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter organizações do usuário {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<OrganizationUserDto>> GetOrganizationUsersAsync(int organizationId)
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.Organization)
                .Where(u => u.OrganizationId == organizationId && u.Status == OrganizationUserStatus.Active)
                .ToListAsync();

            return users.Select(MapToOrganizationUserDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter usuários da organização {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<OrganizationUserDto?> AddUserToOrganizationAsync(int organizationId, string userEmail, OrganizationRole role, string currentUserId)
    {
        try
        {
            // Verificar se a organização existe
            var organization = await _context.Organizations
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                throw new ArgumentException("Organização não encontrada");
            }

            // Verificar permissões do usuário atual
            var currentUser = organization.Users
                .FirstOrDefault(u => u.Id == currentUserId && u.Status == OrganizationUserStatus.Active);

            if (currentUser == null || (currentUser.Role != OrganizationRole.Owner && currentUser.Role != OrganizationRole.Admin))
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para adicionar usuários a esta organização");
            }

            // Buscar usuário por email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                throw new ArgumentException("Usuário não encontrado");
            }

            // Verificar se o usuário já tem uma organização
            if (user.OrganizationId.HasValue)
            {
                if (user.OrganizationId == organizationId && user.Status == OrganizationUserStatus.Active)
                {
                    throw new ArgumentException("Usuário já está ativo nesta organização");
                }
                else if (user.OrganizationId != organizationId)
                {
                    throw new ArgumentException("Usuário já pertence a outra organização");
                }
                
                // Reativar usuário removido
                user.Status = OrganizationUserStatus.Active;
                user.Role = role;
                user.JoinedAt = DateTimeOffset.UtcNow;
                user.RemovedAt = null;
            }
            else
            {
                // Adicionar usuário à organização
                user.OrganizationId = organizationId;
                user.Role = role;
                user.Status = OrganizationUserStatus.Active;
                user.JoinedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário {UserId} adicionado à organização {OrganizationId} por {CurrentUserId}", user.Id, organizationId, currentUserId);

            // Recarregar com todas as relações
            var updatedUser = await _context.Users
                .Include(u => u.Organization)
                .FirstAsync(u => u.Id == user.Id);

            return MapToOrganizationUserDto(updatedUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar usuário à organização {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<OrganizationUserDto?> UpdateOrganizationUserAsync(int organizationId, string userId, OrganizationRole role, OrganizationUserStatus status, string currentUserId)
    {
        try
        {
            // Verificar se a organização existe
            var organization = await _context.Organizations
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                throw new ArgumentException("Organização não encontrada");
            }

            // Verificar permissões do usuário atual
            var currentUser = organization.Users
                .FirstOrDefault(u => u.Id == currentUserId && u.Status == OrganizationUserStatus.Active);

            if (currentUser == null || (currentUser.Role != OrganizationRole.Owner && currentUser.Role != OrganizationRole.Admin))
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para atualizar usuários desta organização");
            }

            // Buscar o usuário na organização
            var user = organization.Users
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                throw new ArgumentException("Usuário não encontrado na organização");
            }

            // Não permitir que o próprio usuário altere seu próprio status/role se for o único Owner
            if (userId == currentUserId && currentUser.Role == OrganizationRole.Owner)
            {
                var ownerCount = organization.Users
                    .Count(u => u.Role == OrganizationRole.Owner && u.Status == OrganizationUserStatus.Active);
                
                if (ownerCount == 1 && (role != OrganizationRole.Owner || status != OrganizationUserStatus.Active))
                {
                    throw new InvalidOperationException("Não é possível alterar o último proprietário da organização");
                }
            }

            user.Role = role;
            user.Status = status;

            if (status == OrganizationUserStatus.Removed)
            {
                user.RemovedAt = DateTimeOffset.UtcNow;
            }
            else if (status == OrganizationUserStatus.Active && user.RemovedAt.HasValue)
            {
                user.RemovedAt = null;
            }

            await _context.SaveChangesAsync();

            // Recarregar com todas as relações
            var updatedUser = await _context.Users
                .Include(u => u.Organization)
                .FirstAsync(u => u.Id == user.Id);

            _logger.LogInformation("Usuário {UserId} atualizado na organização {OrganizationId} por {CurrentUserId}", 
                userId, organizationId, currentUserId);

            return MapToOrganizationUserDto(updatedUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar usuário {UserId} na organização {OrganizationId}", userId, organizationId);
            throw;
        }
    }

    public async Task<bool> RemoveUserFromOrganizationAsync(int organizationId, string userId, string currentUserId)
    {
        try
        {
            // Verificar se a organização existe
            var organization = await _context.Organizations
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return false;
            }

            // Buscar o usuário na organização
            var user = organization.Users
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return false;
            }

            // Verificar permissões do usuário atual
            var currentUser = organization.Users
                .FirstOrDefault(u => u.Id == currentUserId && u.Status == OrganizationUserStatus.Active);

            if (currentUser == null || (currentUser.Role != OrganizationRole.Owner && currentUser.Role != OrganizationRole.Admin))
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para remover usuários desta organização");
            }

            // Não permitir que o último Owner seja removido
            if (user.Role == OrganizationRole.Owner)
            {
                var ownerCount = organization.Users
                    .Count(u => u.Role == OrganizationRole.Owner && u.Status == OrganizationUserStatus.Active);

                if (ownerCount <= 1)
                {
                    throw new InvalidOperationException("Não é possível remover o último proprietário da organização");
                }
            }

            user.Status = OrganizationUserStatus.Removed;
            user.RemovedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário {UserId} removido da organização {OrganizationId} por {CurrentUserId}", 
                userId, organizationId, currentUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover usuário {UserId} da organização {OrganizationId}", userId, organizationId);
            throw;
        }
    }

    private static OrganizationDto MapToDto(Organization organization)
    {
        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            //Description = organization.Description,
            ContactEmail = organization.ContactEmail,
            ContactPhone = organization.ContactPhone,
            Address = organization.Address,
            Status = organization.Status.ToString(),
            CreatedAt = organization.CreatedAt,
            Users = organization.Users?.Select(MapToOrganizationUserDto).ToList() ?? [],
            Workspaces = organization.Workspaces?.Select(MapToWorkspaceDto).ToList() ?? []
        };
    }

    private static OrganizationUserDto MapToOrganizationUserDto(ApplicationUser user)
    {
        return new OrganizationUserDto
        {
            Id = 0, // Não há mais um Id específico para OrganizationUser
            OrganizationId = user.OrganizationId ?? 0,
            OrganizationName = user.Organization?.Name ?? string.Empty,
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            UserEmail = user.Email ?? string.Empty,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            JoinedAt = user.JoinedAt ?? DateTimeOffset.UtcNow,
            RemovedAt = user.RemovedAt
        };
    }

    private static WorkspaceDto MapToWorkspaceDto(Workspace workspace)
    {
        return new WorkspaceDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            OrganizationId = workspace.OrganizationId,
            OrganizationName = workspace.Organization?.Name ?? string.Empty,
            Status = workspace.Status.ToString(),
            CreatedAt = workspace.CreatedAt,
            Users = workspace.WorkspaceUsers?.Select(wu => new WorkspaceUserDto
            {
                Id = wu.Id,
                WorkspaceId = wu.WorkspaceId,
                WorkspaceName = wu.Workspace?.Name ?? string.Empty,
                UserId = wu.UserId,
                UserName = wu.User?.UserName ?? string.Empty,
                UserEmail = wu.User?.Email ?? string.Empty,
                Role = wu.Role,
                Status = wu.Status,
                JoinedAt = wu.JoinedAt,
                RemovedAt = wu.RemovedAt
            }).ToList() ?? []
        };
    }
}