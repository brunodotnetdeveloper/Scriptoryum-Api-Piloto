using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Dtos;

public class OrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; }
    public string Address { get; set; }
    public string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<OrganizationUserDto> Users { get; set; } = [];
    public List<WorkspaceDto> Workspaces { get; set; } = [];
}

public class OrganizationUserDto
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; }
    public string Status { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
}

public class WorkspaceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<WorkspaceUserDto> Users { get; set; } = [];
}

public class WorkspaceUserDto
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string WorkspaceName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; }
    public string Status { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
}

public class CreateOrganizationDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; }
    public string Address { get; set; }
}

public class UpdateOrganizationDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; }
    public string Address { get; set; }
    public string Status { get; set; }
}

public class CreateWorkspaceDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; }
}

public class UpdateWorkspaceDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; }
    public string Status { get; set; }
}

public class UpdateWorkspaceUserDto
{
    public string Role { get; set; }
    public string Status { get; set; }
}

public class AddUserToWorkspaceDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; }
}