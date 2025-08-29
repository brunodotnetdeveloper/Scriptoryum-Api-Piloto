using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Context;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Scriptoryum.Api.Application.Services;

public interface IWorkspaceService
{
    Task<WorkspaceDto> CreateWorkspaceAsync(CreateWorkspaceDto createDto, string userId);
    Task<IEnumerable<WorkspaceDto>> GetAllWorkspacesAsync();
    Task<IEnumerable<WorkspaceDto>> GetUserWorkspacesAsync(string userId);
    Task<WorkspaceDto?> GetWorkspaceByIdAsync(int workspaceId, string userId);
    Task<WorkspaceDto?> UpdateWorkspaceAsync(int workspaceId, UpdateWorkspaceDto updateDto, string userId);
    Task<bool> DeleteWorkspaceAsync(int workspaceId, string userId);
    Task<WorkspaceUserDto> AddUserToWorkspaceAsync(int workspaceId, AddUserToWorkspaceDto addUserDto, string currentUserId);
    Task<WorkspaceUserDto?> UpdateWorkspaceUserAsync(int workspaceId, string userId, UpdateWorkspaceUserDto updateDto, string currentUserId);
    Task<bool> RemoveUserFromWorkspaceAsync(int workspaceId, string userId, string currentUserId);
    Task<IEnumerable<WorkspaceUserDto>> GetWorkspaceUsersAsync(int workspaceId, string currentUserId);
}

public class WorkspaceService(ScriptoryumDbContext context, ILogger<WorkspaceService> logger, UserManager<ApplicationUser> userManager, INotificationService notificationService) : IWorkspaceService
{
    private readonly ScriptoryumDbContext _context = context;
    private readonly ILogger<WorkspaceService> _logger = logger;

    public async Task<IEnumerable<WorkspaceDto>> GetAllWorkspacesAsync()
    {
        try
        {
            var workspaces = await _context.Workspaces
                .Include(w => w.Organization)
                .Include(w => w.WorkspaceUsers)
                    .ThenInclude(wu => wu.User)
                .Where(w => w.Status != WorkspaceStatus.Deleted)
                .ToListAsync();

            return workspaces.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todos os workspaces");
            throw;
        }
    }

    public async Task<WorkspaceDto?> GetWorkspaceByIdAsync(int workspaceId, string userId)
    {
        try
        {
            var workspace = await _context.Workspaces
                .Include(w => w.Organization)
                .Include(w => w.WorkspaceUsers)
                    .ThenInclude(wu => wu.User)
                .FirstOrDefaultAsync(w => w.Id == workspaceId && w.Status != WorkspaceStatus.Deleted);

            return workspace != null ? MapToDto(workspace) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter workspace por ID {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<WorkspaceDto> CreateWorkspaceAsync(CreateWorkspaceDto createDto, string userId)
    {
        try
        {
            // Obter o usuário e verificar se ele pertence a uma organização
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new ArgumentException("Usuário não encontrado");
            }

            if (user.OrganizationId == null)
            {
                throw new ArgumentException("Usuário não pertence a nenhuma organização");
            }

            // Verificar se a organização existe
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == user.OrganizationId);

            if (organization == null)
            {
                throw new ArgumentException("Organização não encontrada");
            }

            // Verificar se o usuário tem permissão na organização
            var userInOrg = user.Status == OrganizationUserStatus.Active &&
                           (user.Role == OrganizationRole.Owner || user.Role == OrganizationRole.Admin);

            if (!userInOrg)
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para criar workspaces nesta organização");
            }

            var workspace = new Workspace
            {
                Name = createDto.Name,
                Description = createDto.Description,
                OrganizationId = user.OrganizationId.Value,
                Status = WorkspaceStatus.Active
            };

            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            // Adicionar o usuário criador como Owner do workspace
            var workspaceUser = new WorkspaceUser
            {
                WorkspaceId = workspace.Id,
                UserId = userId,
                Role = WorkspaceRole.Admin.ToString(),
                Status = WorkspaceUserStatus.Active.ToString()
            };

            _context.WorkspaceUsers.Add(workspaceUser);
            await _context.SaveChangesAsync();

            // Recarregar com includes
            var createdWorkspace = await _context.Workspaces
                .Include(w => w.Organization)
                .Include(w => w.WorkspaceUsers)
                    .ThenInclude(wu => wu.User)
                .FirstAsync(w => w.Id == workspace.Id);

            _logger.LogInformation("Workspace {WorkspaceName} criado com sucesso por usuário {UserId}", createDto.Name, userId);
            return MapToDto(createdWorkspace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar workspace {WorkspaceName}", createDto.Name);
            throw;
        }
    }

    public async Task<WorkspaceDto?> UpdateWorkspaceAsync(int id, UpdateWorkspaceDto updateDto, string userId)
    {
        try
        {
            var workspace = await _context.Workspaces
                .Include(w => w.Organization)
                .Include(w => w.WorkspaceUsers)
                    .ThenInclude(wu => wu.User)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workspace == null)
            {
                return null;
            }

            // Verificar permissões
            var userWorkspace = workspace.WorkspaceUsers
                .FirstOrDefault(wu => wu.UserId == userId && wu.Status == WorkspaceUserStatus.Active.ToString());

            if (userWorkspace == null || userWorkspace.Role != WorkspaceRole.Admin.ToString())
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para atualizar este workspace");
            }

            workspace.Name = updateDto.Name;
            workspace.Description = updateDto.Description;
            
            if (Enum.TryParse<WorkspaceStatus>(updateDto.Status, out var status))
            {
                workspace.Status = status;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Workspace {WorkspaceId} atualizado por usuário {UserId}", id, userId);
            return MapToDto(workspace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar workspace {WorkspaceId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteWorkspaceAsync(int id, string userId)
    {
        try
        {
            var workspace = await _context.Workspaces
                .Include(w => w.WorkspaceUsers)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workspace == null)
            {
                return false;
            }

            // Verificar se o usuário é owner
            var userWorkspace = workspace.WorkspaceUsers
                .FirstOrDefault(wu => wu.UserId == userId && wu.Status == WorkspaceUserStatus.Active.ToString());

            if (userWorkspace?.Role != WorkspaceRole.Admin.ToString())
            {
                throw new UnauthorizedAccessException("Apenas o owner pode deletar o workspace");
            }

            workspace.Status = WorkspaceStatus.Deleted;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Workspace {WorkspaceId} deletado por usuário {UserId}", id, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar workspace {WorkspaceId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<WorkspaceDto>> GetUserWorkspacesAsync(string userId)
    {
        try
        {
            var workspaces = await _context.WorkspaceUsers
                .Include(wu => wu.Workspace)
                    .ThenInclude(w => w.Organization)
                .Include(wu => wu.Workspace.WorkspaceUsers)
                    .ThenInclude(wu => wu.User)
                .Where(wu => wu.UserId == userId && wu.Status == WorkspaceUserStatus.Active.ToString())
                .Select(wu => wu.Workspace)
                .Where(w => w.Status != WorkspaceStatus.Deleted)
                .ToListAsync();

            return workspaces.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter workspaces do usuário {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<WorkspaceUserDto>> GetWorkspaceUsersAsync(int workspaceId, string currentUserId)
    {
        try
        {
            var workspaceUsers = await _context.WorkspaceUsers
                .Include(wu => wu.User)
                .Include(wu => wu.Workspace)
                .Where(wu => wu.WorkspaceId == workspaceId && wu.Status != WorkspaceUserStatus.Removed.ToString())
                .ToListAsync();

            return workspaceUsers.Select(MapToWorkspaceUserDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter usuários do workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<WorkspaceUserDto?> AddUserToWorkspaceAsync(int workspaceId, AddUserToWorkspaceDto addUserDto, string currentUserId)
    {
        try
        {
            // Verificar se o workspace existe
            var workspace = await _context.Workspaces
                .Include(w => w.WorkspaceUsers)
                .FirstOrDefaultAsync(w => w.Id == workspaceId);

            if (workspace == null)
            {
                throw new ArgumentException("Workspace não encontrado");
            }

            // Verificar permissões do usuário atual
            var currentUserWorkspace = workspace.WorkspaceUsers
                .FirstOrDefault(wu => wu.UserId == currentUserId && wu.Status == WorkspaceUserStatus.Active.ToString());

            if (currentUserWorkspace == null || currentUserWorkspace.Role != WorkspaceRole.Admin.ToString())
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para adicionar usuários a este workspace");
            }

            // Buscar usuário por email (case-insensitive)
            _logger.LogInformation("Buscando usuário com email: {Email}", addUserDto.UserEmail);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == addUserDto.UserEmail.ToUpper());
            
            if (user == null)
            {
                _logger.LogInformation("Usuário não encontrado com email: {Email}. Criando automaticamente...", addUserDto.UserEmail);
                
                // Criar usuário automaticamente
                user = await CreateUserAutomaticallyAsync(addUserDto.UserEmail);
                
                _logger.LogInformation("Usuário criado automaticamente: {UserId} - {UserName}", user.Id, user.UserName);
            }
            else
            {
                _logger.LogInformation("Usuário encontrado: {UserId} - {UserName}", user.Id, user.UserName);
            }

            // Verificar se o usuário já está no workspace
            var existingWorkspaceUser = await _context.WorkspaceUsers
                .FirstOrDefaultAsync(wu => wu.WorkspaceId == workspaceId && wu.UserId == user.Id);

            if (existingWorkspaceUser != null)
            {
                if (existingWorkspaceUser.Status == WorkspaceUserStatus.Active.ToString())
                {
                    throw new InvalidOperationException("Usuário já está ativo neste workspace");
                }
                
                // Reativar usuário
                existingWorkspaceUser.Status = WorkspaceUserStatus.Active.ToString();
                existingWorkspaceUser.Role = addUserDto.Role.ToString();
                existingWorkspaceUser.JoinedAt = DateTimeOffset.UtcNow;
                existingWorkspaceUser.RemovedAt = null;
            }
            else
            {
                // Criar nova relação
                existingWorkspaceUser = new WorkspaceUser
                {
                    WorkspaceId = workspaceId,
                    UserId = user.Id,
                    Role = addUserDto.Role.ToString(),
                    Status = WorkspaceUserStatus.Active.ToString()
                };
                _context.WorkspaceUsers.Add(existingWorkspaceUser);
            }

            await _context.SaveChangesAsync();

            // Recarregar com includes
            var workspaceUser = await _context.WorkspaceUsers
                .Include(wu => wu.User)
                .Include(wu => wu.Workspace)
                .FirstAsync(wu => wu.Id == existingWorkspaceUser.Id);

            _logger.LogInformation("Usuário {UserEmail} adicionado ao workspace {WorkspaceId}", addUserDto.UserEmail, workspaceId);
            return MapToWorkspaceUserDto(workspaceUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar usuário {UserEmail} ao workspace {WorkspaceId}", addUserDto.UserEmail, workspaceId);
            throw;
        }
    }

    public async Task<WorkspaceUserDto?> UpdateWorkspaceUserAsync(int workspaceId, string userId, UpdateWorkspaceUserDto updateDto, string currentUserId)
    {
        try
        {
            var workspaceUser = await _context.WorkspaceUsers
                .Include(wu => wu.User)
                .Include(wu => wu.Workspace)
                .FirstOrDefaultAsync(wu => wu.WorkspaceId == workspaceId && wu.UserId == userId);

            if (workspaceUser == null)
            {
                return null;
            }

            // Verificar permissões
            var currentUserWorkspace = await _context.WorkspaceUsers
                .FirstOrDefaultAsync(wu => wu.WorkspaceId == workspaceId && wu.UserId == currentUserId && wu.Status == WorkspaceUserStatus.Active.ToString());

            if (currentUserWorkspace == null || currentUserWorkspace.Role != WorkspaceRole.Admin.ToString())
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para atualizar usuários deste workspace");
            }

            workspaceUser.Role = updateDto.Role.ToString();
            workspaceUser.Status = updateDto.Status.ToString();

            if (updateDto.Status == WorkspaceUserStatus.Removed.ToString())
            {
                workspaceUser.RemovedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário {UserId} atualizado no workspace {WorkspaceId}", userId, workspaceId);
            return MapToWorkspaceUserDto(workspaceUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar usuário {UserId} no workspace {WorkspaceId}", userId, workspaceId);
            throw;
        }
    }

    public async Task<bool> RemoveUserFromWorkspaceAsync(int workspaceId, string userId, string currentUserId)
    {
        try
        {
            var workspaceUser = await _context.WorkspaceUsers
                .FirstOrDefaultAsync(wu => wu.WorkspaceId == workspaceId && wu.UserId == userId);

            if (workspaceUser == null)
            {
                return false;
            }

            // Verificar permissões
            var currentUserWorkspace = await _context.WorkspaceUsers
                .FirstOrDefaultAsync(wu => wu.WorkspaceId == workspaceId && wu.UserId == currentUserId && wu.Status == WorkspaceUserStatus.Active.ToString());

            if (currentUserWorkspace == null || currentUserWorkspace.Role != WorkspaceRole.Admin.ToString())
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para remover usuários deste workspace");
            }

            // Não permitir remover o último owner
            if (workspaceUser.Role == WorkspaceRole.Admin.ToString())
            {
                var adminCount = await _context.WorkspaceUsers
                    .CountAsync(wu => wu.WorkspaceId == workspaceId && wu.Role == WorkspaceRole.Admin.ToString() && wu.Status == WorkspaceUserStatus.Active.ToString());

                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("Não é possível remover o último owner do workspace");
                }
            }

            workspaceUser.Status = WorkspaceUserStatus.Removed.ToString();
            workspaceUser.RemovedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário {UserId} removido do workspace {WorkspaceId}", userId, workspaceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover usuário {UserId} do workspace {WorkspaceId}", userId, workspaceId);
            throw;
        }
    }

    private static WorkspaceDto MapToDto(Workspace workspace)
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
            Users = workspace.WorkspaceUsers?.Select(MapToWorkspaceUserDto).ToList() ?? []
        };
    }

    private static WorkspaceUserDto MapToWorkspaceUserDto(WorkspaceUser workspaceUser)
    {
        return new WorkspaceUserDto
        {
            Id = workspaceUser.Id,
            WorkspaceId = workspaceUser.WorkspaceId,
            WorkspaceName = workspaceUser.Workspace?.Name ?? string.Empty,
            UserId = workspaceUser.UserId,
            UserName = workspaceUser.User?.UserName ?? string.Empty,
            UserEmail = workspaceUser.User?.Email ?? string.Empty,
            Role = workspaceUser.Role,
            Status = workspaceUser.Status.ToString(),
            JoinedAt = workspaceUser.JoinedAt,
            RemovedAt = workspaceUser.RemovedAt
        };
    }

    private async Task<ApplicationUser> CreateUserAutomaticallyAsync(string email)
    {
        try
        {
            // Gerar senha temporária
            var temporaryPassword = GenerateTemporaryPassword();
            
            // Extrair nome de usuário do email
            var userName = email.Split('@')[0];
            
            // Criar novo usuário
            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(), // Gerar ID único
                UserName = userName,
                Email = email,
                EmailConfirmed = false // Usuário precisará confirmar email
            };
            
            // Criar usuário usando UserManager
            var result = await userManager.CreateAsync(newUser, temporaryPassword);
            
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Erro ao criar usuário automaticamente: {Errors}", errors);
                throw new InvalidOperationException($"Erro ao criar usuário: {errors}");
            }
            
            _logger.LogInformation("Usuário criado com sucesso: {UserId} - {Email}", newUser.Id, newUser.Email);
            
            // Enviar notificação por email com credenciais
            await SendWelcomeNotificationAsync(newUser, temporaryPassword);
            
            return newUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário automaticamente para email: {Email}", email);
            throw;
        }
    }
    
    private static string GenerateTemporaryPassword()
    {
        // Gerar senha temporária segura
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        var password = new char[12];
        
        // Garantir pelo menos um caractere de cada tipo
        password[0] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[random.Next(26)];
        password[1] = "abcdefghijklmnopqrstuvwxyz"[random.Next(26)];
        password[2] = "0123456789"[random.Next(10)];
        password[3] = "!@#$%"[random.Next(5)];
        
        // Preencher o resto aleatoriamente
        for (int i = 4; i < 12; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }
        
        // Embaralhar a senha
        for (int i = 0; i < password.Length; i++)
        {
            int j = random.Next(i, password.Length);
            (password[i], password[j]) = (password[j], password[i]);
        }
        
        return new string(password);
    }
    
    private async Task SendWelcomeNotificationAsync(ApplicationUser user, string temporaryPassword)
    {
        try
        {
            var notification = new CreateNotificationDto
            {
                UserId = user.Id,
                Title = "Bem-vindo ao Scriptoryum!",
                Message = $"Sua conta foi criada automaticamente. Suas credenciais temporárias são:\n\nEmail: {user.Email}\nSenha: {temporaryPassword}\n\nPor favor, faça login e altere sua senha o mais breve possível.",
                Type = "Welcome"
            };
            
            await notificationService.CreateNotificationAsync(notification);
            _logger.LogInformation("Notificação de boas-vindas enviada para: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação de boas-vindas para: {Email}", user.Email);
            // Não falhar o processo principal se a notificação falhar
        }
    }
}