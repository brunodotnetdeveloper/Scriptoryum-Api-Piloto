using Microsoft.EntityFrameworkCore;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Interfaces;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Context;
using System.Text.Json;

namespace Scriptoryum.Api.Application.Services;

/// <summary>
/// Serviço para gestão de templates de tipos de documentos
/// </summary>
public class DocumentTypeTemplateService : IDocumentTypeTemplateService
{
    private readonly ScriptoryumDbContext _context;
    private readonly ILogger<DocumentTypeTemplateService> _logger;

    public DocumentTypeTemplateService(ScriptoryumDbContext context, ILogger<DocumentTypeTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region CRUD de Templates

    public async Task<DocumentTypeTemplateDto?> GetByIdAsync(int id, int organizationId)
    {
        var template = await _context.DocumentTypeTemplates
            .Include(t => t.Organization)
            .Where(t => t.Id == id && (t.OrganizationId == organizationId || t.IsPublic))
            .FirstOrDefaultAsync();

        return template != null ? MapToDto(template) : null;
    }

    public async Task<IEnumerable<DocumentTypeTemplateDto>> GetAllAsync(int organizationId, bool includePublic = true)
    {
        var query = _context.DocumentTypeTemplates
            .Include(t => t.Organization)
            .Where(t => t.OrganizationId == organizationId);

        if (includePublic)
        {
            query = _context.DocumentTypeTemplates
                .Include(t => t.Organization)
                .Where(t => t.OrganizationId == organizationId || t.IsPublic);
        }

        var templates = await query
            .OrderBy(t => t.Name)
            .ToListAsync();

        return templates.Select(MapToDto);
    }

    public async Task<IEnumerable<DocumentTypeTemplateDto>> GetByCategory(string category, int organizationId)
    {
        var templates = await _context.DocumentTypeTemplates
            .Include(t => t.Organization)
            .Where(t => (t.OrganizationId == organizationId || t.IsPublic))
            .ToListAsync();

        var filteredTemplates = templates.Where(t =>
        {
            try
            {
                var templateData = JsonSerializer.Deserialize<DocumentTypeTemplateData>(t.TemplateData);
                return templateData?.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true;
            }
            catch
            {
                return false;
            }
        });

        return filteredTemplates.Select(MapToDto).OrderBy(t => t.Name);
    }

    public async Task<IEnumerable<DocumentTypeTemplateDto>> GetPublicTemplatesAsync()
    {
        var templates = await _context.DocumentTypeTemplates
            .Include(t => t.Organization)
            .Where(t => t.IsPublic)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return templates.Select(MapToDto);
    }

    public async Task<DocumentTypeTemplateDto> CreateAsync(CreateDocumentTypeTemplateDto dto, int organizationId, string userId)
    {
        // Validar se o nome já existe
        if (await TemplateNameExistsAsync(dto.Name, organizationId))
        {
            throw new InvalidOperationException($"Já existe um template com o nome '{dto.Name}' nesta organização.");
        }

        // Validar dados do template
        var validationResult = ValidateTemplateData(dto);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Dados inválidos: {string.Join(", ", validationResult.Errors)}");
        }

        var templateData = new DocumentTypeTemplateData
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Fields = dto.Fields.Select((field, index) => new DocumentTypeFieldTemplate
            {
                FieldName = field.FieldName,
                FieldType = field.FieldType.ToString(),
                Description = field.Description,
                ExtractionPrompt = field.ExtractionPrompt,
                IsRequired = field.IsRequired,
                ValidationRegex = field.ValidationRegex,
                DefaultValue = field.DefaultValue,
                FieldOrder = field.FieldOrder > 0 ? field.FieldOrder : index + 1
            }).ToList()
        };

        var template = new DocumentTypeTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            TemplateData = JsonSerializer.Serialize(templateData),
            IsPublic = dto.IsPublic,
            OrganizationId = organizationId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentTypeTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{Name}' criado com sucesso para organização {OrganizationId} por usuário {UserId}", 
            dto.Name, organizationId, userId);

        return MapToDto(template);
    }

    public async Task<DocumentTypeTemplateDto> UpdateAsync(int id, CreateDocumentTypeTemplateDto dto, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == id && t.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (template == null)
        {
            throw new InvalidOperationException("Template não encontrado.");
        }

        // Validar se o nome já existe (excluindo o atual)
        if (await TemplateNameExistsAsync(dto.Name, organizationId, id))
        {
            throw new InvalidOperationException($"Já existe um template com o nome '{dto.Name}' nesta organização.");
        }

        // Validar dados do template
        var validationResult = ValidateTemplateData(dto);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Dados inválidos: {string.Join(", ", validationResult.Errors)}");
        }

        var templateData = new DocumentTypeTemplateData
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Fields = dto.Fields.Select((field, index) => new DocumentTypeFieldTemplate
            {
                FieldName = field.FieldName,
                FieldType = field.FieldType.ToString(),
                Description = field.Description,
                ExtractionPrompt = field.ExtractionPrompt,
                IsRequired = field.IsRequired,
                ValidationRegex = field.ValidationRegex,
                DefaultValue = field.DefaultValue,
                FieldOrder = field.FieldOrder > 0 ? field.FieldOrder : index + 1
            }).ToList()
        };

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.TemplateData = JsonSerializer.Serialize(templateData);
        template.IsPublic = dto.IsPublic;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{Name}' (ID: {Id}) atualizado com sucesso por usuário {UserId}", 
            dto.Name, id, userId);

        return MapToDto(template);
    }

    public async Task<bool> DeleteAsync(int id, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == id && t.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (template == null)
            return false;

        // Verificar se pode ser deletado (não está sendo usado)
        if (!await CanDeleteTemplateAsync(id, organizationId))
        {
            throw new InvalidOperationException("Não é possível excluir este template pois está sendo usado.");
        }

        _context.DocumentTypeTemplates.Remove(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{Name}' (ID: {Id}) excluído com sucesso por usuário {UserId}", 
            template.Name, id, userId);

        return true;
    }

    #endregion

    #region Aplicação de Templates

    public async Task<DocumentTypeDto> ApplyTemplateAsync(ApplyTemplateDto dto, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == dto.TemplateId && (t.OrganizationId == organizationId || t.IsPublic))
            .FirstOrDefaultAsync();

        if (template == null)
        {
            throw new InvalidOperationException("Template não encontrado.");
        }

        DocumentTypeTemplateData? templateData;
        try
        {
            templateData = JsonSerializer.Deserialize<DocumentTypeTemplateData>(template.TemplateData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deserializar dados do template {TemplateId}", dto.TemplateId);
            throw new InvalidOperationException("Dados do template inválidos.");
        }

        if (templateData == null)
        {
            throw new InvalidOperationException("Dados do template inválidos.");
        }

        // Verificar se o nome do tipo de documento já existe
        var documentTypeExists = await _context.DocumentTypes
            .AnyAsync(dt => dt.Name == dto.DocumentTypeName && dt.OrganizationId == organizationId);

        if (documentTypeExists)
        {
            throw new InvalidOperationException($"Já existe um tipo de documento com o nome '{dto.DocumentTypeName}' nesta organização.");
        }

        // Criar tipo de documento baseado no template
        var documentType = new DocumentType
        {
            Name = dto.DocumentTypeName,
            Description = dto.DocumentTypeDescription ?? templateData.Description,
            OrganizationId = organizationId,
            IsSystemDefault = false,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Fields = templateData.Fields.Select(f => new DocumentTypeField
            {
                FieldName = f.FieldName,
                FieldType = f.FieldType.ToString(),
                Description = f.Description,
                ExtractionPrompt = f.ExtractionPrompt,
                IsRequired = f.IsRequired,
                ValidationRegex = f.ValidationRegex,
                DefaultValue = f.DefaultValue,
                FieldOrder = f.FieldOrder,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList()
        };

        _context.DocumentTypes.Add(documentType);
        await _context.SaveChangesAsync();

        // Incrementar contador de uso do template (se implementado)
        await IncrementTemplateUsageAsync(dto.TemplateId);

        _logger.LogInformation("Template '{TemplateName}' aplicado para criar tipo de documento '{DocumentTypeName}' por usuário {UserId}", 
            template.Name, dto.DocumentTypeName, userId);

        // Retornar DTO do tipo de documento criado
        var createdDocumentType = await _context.DocumentTypes
            .Include(dt => dt.Fields)
            .Include(dt => dt.Organization)
            .FirstAsync(dt => dt.Id == documentType.Id);

        return MapDocumentTypeToDto(createdDocumentType);
    }

    public async Task<DocumentTypeTemplateDto> CreateTemplateFromDocumentTypeAsync(int documentTypeId, 
        string templateName, string? templateDescription, string? category, bool isPublic, 
        int organizationId, string userId)
    {
        var documentType = await _context.DocumentTypes
            .Include(dt => dt.Fields.Where(f => f.Status == "Active"))
            .Where(dt => dt.Id == documentTypeId && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
        {
            throw new InvalidOperationException("Tipo de documento não encontrado.");
        }

        // Verificar se o nome do template já existe
        if (await TemplateNameExistsAsync(templateName, organizationId))
        {
            throw new InvalidOperationException($"Já existe um template com o nome '{templateName}' nesta organização.");
        }

        var templateData = new DocumentTypeTemplateData
        {
            Name = templateName,
            Description = templateDescription ?? documentType.Description,
            Category = category ?? "Personalizado",
            Fields = documentType.Fields.Select(f => new DocumentTypeFieldTemplate
            {
                FieldName = f.FieldName,
                FieldType = f.FieldType.ToString(),
                Description = f.Description,
                ExtractionPrompt = f.ExtractionPrompt,
                IsRequired = f.IsRequired,
                ValidationRegex = f.ValidationRegex,
                DefaultValue = f.DefaultValue,
                FieldOrder = f.FieldOrder
            }).ToList()
        };

        var template = new DocumentTypeTemplate
        {
            Name = templateName,
            Description = templateDescription ?? $"Template baseado no tipo de documento '{documentType.Name}'",
            TemplateData = JsonSerializer.Serialize(templateData),
            IsPublic = isPublic,
            OrganizationId = organizationId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentTypeTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{TemplateName}' criado a partir do tipo de documento '{DocumentTypeName}' por usuário {UserId}", 
            templateName, documentType.Name, userId);

        return MapToDto(template);
    }

    #endregion

    #region Gestão de Templates Públicos/Privados

    public async Task<bool> MakePublicAsync(int id, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == id && t.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (template == null)
            return false;

        template.IsPublic = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{Name}' (ID: {Id}) tornado público por usuário {UserId}", 
            template.Name, id, userId);

        return true;
    }

    public async Task<bool> MakePrivateAsync(int id, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == id && t.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (template == null)
            return false;

        template.IsPublic = false;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{Name}' (ID: {Id}) tornado privado por usuário {UserId}", 
            template.Name, id, userId);

        return true;
    }

    public async Task<DocumentTypeTemplateDto> DuplicateTemplateAsync(int id, string newName, 
        int organizationId, string userId)
    {
        var originalTemplate = await _context.DocumentTypeTemplates
            .Where(t => t.Id == id && (t.OrganizationId == organizationId || t.IsPublic))
            .FirstOrDefaultAsync();

        if (originalTemplate == null)
        {
            throw new InvalidOperationException("Template não encontrado.");
        }

        // Verificar se o novo nome já existe
        if (await TemplateNameExistsAsync(newName, organizationId))
        {
            throw new InvalidOperationException($"Já existe um template com o nome '{newName}' nesta organização.");
        }

        var duplicatedTemplate = new DocumentTypeTemplate
        {
            Name = newName,
            Description = $"Cópia de: {originalTemplate.Description}",
            TemplateData = originalTemplate.TemplateData,
            IsPublic = false, // Cópias sempre começam como privadas
            OrganizationId = organizationId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentTypeTemplates.Add(duplicatedTemplate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{OriginalName}' duplicado como '{NewName}' por usuário {UserId}", 
            originalTemplate.Name, newName, userId);

        return MapToDto(duplicatedTemplate);
    }

    #endregion

    #region Consultas e Relatórios

    public async Task<IEnumerable<string>> GetCategoriesAsync(int organizationId)
    {
        var templates = await _context.DocumentTypeTemplates
            .Where(t => t.OrganizationId == organizationId || t.IsPublic)
            .ToListAsync();

        var categories = new HashSet<string>();

        foreach (var template in templates)
        {
            try
            {
                var templateData = JsonSerializer.Deserialize<DocumentTypeTemplateData>(template.TemplateData);
                if (!string.IsNullOrEmpty(templateData?.Category))
                {
                    categories.Add(templateData.Category);
                }
            }
            catch
            {
                // Ignorar templates com dados inválidos
            }
        }

        return categories.OrderBy(c => c);
    }

    public async Task<IEnumerable<DocumentTypeTemplateDto>> SearchTemplatesAsync(string searchTerm, int organizationId)
    {
        var templates = await _context.DocumentTypeTemplates
            .Include(t => t.Organization)
            .Where(t => t.OrganizationId == organizationId || t.IsPublic)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return templates.Select(MapToDto).OrderBy(t => t.Name);
        }

        var filteredTemplates = templates.Where(t =>
            t.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            (t.Description != null && t.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
            TemplateContainsSearchTerm(t.TemplateData, searchTerm)
        );

        return filteredTemplates.Select(MapToDto).OrderBy(t => t.Name);
    }

    public async Task<Dictionary<string, object>> GetTemplateStatisticsAsync(int templateId, int organizationId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == templateId && (t.OrganizationId == organizationId || t.IsPublic))
            .FirstOrDefaultAsync();

        if (template == null)
        {
            return new Dictionary<string, object>();
        }

        // TODO: Implementar contagem real de uso
        var usageCount = 0;
        var lastUsed = (DateTime?)null;

        DocumentTypeTemplateData? templateData = null;
        try
        {
            templateData = JsonSerializer.Deserialize<DocumentTypeTemplateData>(template.TemplateData);
        }
        catch
        {
            // Ignorar erro de deserialização
        }

        return new Dictionary<string, object>
        {
            ["usageCount"] = usageCount,
            ["lastUsed"] = lastUsed,
            ["fieldCount"] = templateData?.Fields?.Count ?? 0,
            ["category"] = templateData?.Category ?? "Sem categoria",
            ["isPublic"] = template.IsPublic,
            ["createdAt"] = template.CreatedAt,
            ["updatedAt"] = template.UpdatedAt
        };
    }

    public async Task<IEnumerable<DocumentTypeTemplateDto>> GetMostUsedTemplatesAsync(int organizationId, int limit = 10)
    {
        // TODO: Implementar ordenação por uso real
        var templates = await _context.DocumentTypeTemplates
            .Include(t => t.Organization)
            .Where(t => t.OrganizationId == organizationId || t.IsPublic)
            .OrderByDescending(t => t.CreatedAt) // Temporariamente ordenar por data de criação
            .Take(limit)
            .ToListAsync();

        return templates.Select(MapToDto);
    }

    #endregion

    #region Validações

    public async Task<bool> TemplateExistsAsync(int templateId, int organizationId)
    {
        return await _context.DocumentTypeTemplates
            .AnyAsync(t => t.Id == templateId && (t.OrganizationId == organizationId || t.IsPublic));
    }

    public async Task<bool> TemplateNameExistsAsync(string name, int organizationId, int? excludeId = null)
    {
        var query = _context.DocumentTypeTemplates
            .Where(t => t.Name == name && t.OrganizationId == organizationId);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> CanDeleteTemplateAsync(int templateId, int organizationId)
    {
        // TODO: Implementar verificação de uso real
        // Por enquanto, sempre permitir exclusão
        return true;
    }

    public ValidationResult ValidateTemplateData(CreateDocumentTypeTemplateDto dto)
    {
        var result = new ValidationResult { IsValid = true };

        // Validar campos
        var fieldNames = new HashSet<string>();
        foreach (var field in dto.Fields)
        {
            if (fieldNames.Contains(field.FieldName))
            {
                result.IsValid = false;
                result.Errors.Add($"Nome de campo duplicado: '{field.FieldName}'.");
            }
            fieldNames.Add(field.FieldName);

            // Validar regex se fornecido
            if (!string.IsNullOrEmpty(field.ValidationRegex))
            {
                try
                {
                    _ = new System.Text.RegularExpressions.Regex(field.ValidationRegex);
                }
                catch
                {
                    result.IsValid = false;
                    result.Errors.Add($"Regex inválido para campo '{field.FieldName}': {field.ValidationRegex}");
                }
            }
        }

        return result;
    }

    public async Task<DocumentTypeDto> ApplyToNewDocumentTypeAsync(int templateId, string documentTypeName, string? description, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == templateId && (t.OrganizationId == organizationId || t.IsPublic))
            .FirstOrDefaultAsync();

        if (template == null)
        {
            throw new InvalidOperationException("Template não encontrado.");
        }

        var templateData = JsonSerializer.Deserialize<DocumentTypeTemplateData>(template.TemplateData);
        if (templateData == null)
        {
            throw new InvalidOperationException("Dados do template inválidos.");
        }

        // Criar novo tipo de documento baseado no template
        var documentType = new DocumentType
        {
            Name = documentTypeName,
            Description = description ?? templateData.Description,
            OrganizationId = organizationId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentTypes.Add(documentType);
        await _context.SaveChangesAsync();

        // Criar campos baseados no template
        foreach (var fieldTemplate in templateData.Fields)
        {
            var field = new DocumentTypeField
            {
                DocumentTypeId = documentType.Id,
                FieldName = fieldTemplate.FieldName,
                FieldType = fieldTemplate.FieldType,
                Description = fieldTemplate.Description,
                ExtractionPrompt = fieldTemplate.ExtractionPrompt,
                IsRequired = fieldTemplate.IsRequired,
                ValidationRegex = fieldTemplate.ValidationRegex,
                DefaultValue = fieldTemplate.DefaultValue,
                FieldOrder = fieldTemplate.FieldOrder,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DocumentTypeFields.Add(field);
        }

        await _context.SaveChangesAsync();

        // Incrementar contador de uso do template
        await IncrementTemplateUsageAsync(templateId);

        _logger.LogInformation("Template '{TemplateName}' aplicado para criar novo tipo de documento '{DocumentTypeName}' por usuário {UserId}",
            template.Name, documentTypeName, userId);

        // Recarregar com relacionamentos
        var createdDocumentType = await _context.DocumentTypes
            .Include(dt => dt.Fields)
            .Include(dt => dt.Organization)
            .FirstAsync(dt => dt.Id == documentType.Id);

        return MapDocumentTypeToDto(createdDocumentType);
    }

    public async Task<DocumentTypeOperationResponseDto> ApplyToExistingDocumentTypeAsync(int templateId, int documentTypeId, bool overwriteFields, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == templateId && (t.OrganizationId == organizationId || t.IsPublic))
            .FirstOrDefaultAsync();

        if (template == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Template não encontrado."
            };
        }

        var documentType = await _context.DocumentTypes
            .Include(dt => dt.Fields)
            .Where(dt => dt.Id == documentTypeId && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Tipo de documento não encontrado."
            };
        }

        var templateData = JsonSerializer.Deserialize<DocumentTypeTemplateData>(template.TemplateData);
        if (templateData == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Dados do template inválidos."
            };
        }

        try
        {
            // Se overwriteFields for true, remover campos existentes
            if (overwriteFields)
            {
                var existingFields = documentType.Fields.ToList();
                _context.DocumentTypeFields.RemoveRange(existingFields);
            }

            // Adicionar campos do template
            foreach (var fieldTemplate in templateData.Fields)
            {
                // Verificar se já existe um campo com o mesmo nome (se não estiver sobrescrevendo)
                if (!overwriteFields && documentType.Fields.Any(f => f.FieldName == fieldTemplate.FieldName))
                {
                    continue; // Pular campo que já existe
                }

                var field = new DocumentTypeField
                {
                    DocumentTypeId = documentType.Id,
                    FieldName = fieldTemplate.FieldName,
                    FieldType = fieldTemplate.FieldType,
                    Description = fieldTemplate.Description,
                    ExtractionPrompt = fieldTemplate.ExtractionPrompt,
                    IsRequired = fieldTemplate.IsRequired,
                    ValidationRegex = fieldTemplate.ValidationRegex,
                    DefaultValue = fieldTemplate.DefaultValue,
                    FieldOrder = fieldTemplate.FieldOrder,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DocumentTypeFields.Add(field);
            }

            documentType.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Incrementar contador de uso do template
            await IncrementTemplateUsageAsync(templateId);

            _logger.LogInformation("Template '{TemplateName}' aplicado ao tipo de documento existente '{DocumentTypeName}' por usuário {UserId}",
                template.Name, documentType.Name, userId);

            return new DocumentTypeOperationResponseDto
            {
                Success = true,
                Message = "Template aplicado com sucesso ao tipo de documento existente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aplicar template {TemplateId} ao tipo de documento {DocumentTypeId}", templateId, documentTypeId);
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Erro interno ao aplicar template."
            };
        }
    }

    public async Task<bool> MakeTemplatePublicAsync(int templateId, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == templateId && t.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (template == null)
            return false;

        template.IsPublic = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{Name}' (ID: {Id}) tornado público por usuário {UserId}",
            template.Name, templateId, userId);

        return true;
    }

    public async Task<bool> MakeTemplatePrivateAsync(int templateId, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == templateId && t.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (template == null)
            return false;

        template.IsPublic = false;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{Name}' (ID: {Id}) tornado privado por usuário {UserId}",
            template.Name, templateId, userId);

        return true;
    }

    public async Task<Dictionary<string, object>> GetTemplateUsageStatisticsAsync(int templateId, int organizationId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == templateId && (t.OrganizationId == organizationId || t.IsPublic))
            .FirstOrDefaultAsync();

        if (template == null)
            return null;

        // Contar quantos tipos de documentos foram criados usando este template
        var documentTypesCreated = await _context.DocumentTypes
            .Where(dt => dt.Name.Contains(template.Name) || dt.Description.Contains(template.Name))
            .CountAsync();

        return new Dictionary<string, object>
        {
            ["TemplateId"] = template.Id,
            ["TemplateName"] = template.Name,
            ["UsageCount"] = template.UsageCount,
            ["DocumentTypesCreated"] = documentTypesCreated,
            ["IsPublic"] = template.IsPublic,
            ["CreatedAt"] = template.CreatedAt,
            ["UpdatedAt"] = template.UpdatedAt
        };
    }



    #endregion

    #region Métodos Auxiliares

    private async Task IncrementTemplateUsageAsync(int templateId)
    {
        // TODO: Implementar incremento de contador de uso
        // Pode ser uma tabela separada ou campo no template
        await Task.CompletedTask;
    }

    private static bool TemplateContainsSearchTerm(string templateDataJson, string searchTerm)
    {
        try
        {
            var templateData = JsonSerializer.Deserialize<DocumentTypeTemplateData>(templateDataJson);
            if (templateData == null) return false;

            // Buscar na categoria
            if (templateData.Category?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                return true;

            // Buscar nos campos
            return templateData.Fields?.Any(f =>
                f.FieldName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (f.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
            ) == true;
        }
        catch
        {
            return false;
        }
    }

    private static DocumentTypeTemplateDto MapToDto(DocumentTypeTemplate template)
    {
        DocumentTypeTemplateData? templateData = null;
        try
        {
            templateData = JsonSerializer.Deserialize<DocumentTypeTemplateData>(template.TemplateData);
        }
        catch
        {
            // Ignorar erro de deserialização
        }

        return new DocumentTypeTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = templateData?.Category,
            IsPublic = template.IsPublic,
            OrganizationId = template.OrganizationId,
            OrganizationName = template.Organization?.Name ?? string.Empty,
            UsageCount = 0, // TODO: Implementar contagem real
            CreatedByUserId = template.CreatedByUserId,
            CreatedByUserName = string.Empty, // TODO: Buscar nome do usuário
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            Fields = templateData?.Fields?.Select(f => new DocumentTypeFieldTemplateDto
            {
                FieldName = f.FieldName,
                FieldType = f.FieldType,
                Description = f.Description,
                ExtractionPrompt = f.ExtractionPrompt,
                IsRequired = f.IsRequired,
                ValidationRegex = f.ValidationRegex,
                DefaultValue = f.DefaultValue,
                FieldOrder = f.FieldOrder
            }).ToList() ?? []
        };
    }

    private static DocumentTypeDto MapDocumentTypeToDto(DocumentType documentType)
    {
        return new DocumentTypeDto
        {
            Id = documentType.Id,
            Name = documentType.Name,
            Description = documentType.Description,
            OrganizationId = documentType.OrganizationId,
            OrganizationName = documentType.Organization?.Name ?? string.Empty,
            IsSystemDefault = documentType.IsSystemDefault,
            Status = documentType.Status,
            CreatedAt = documentType.CreatedAt,
            UpdatedAt = documentType.UpdatedAt,
            Fields = documentType.Fields?.Select(f => new DocumentTypeFieldDto
            {
                Id = f.Id,
                DocumentTypeId = f.DocumentTypeId,
                FieldName = f.FieldName,
                FieldType = f.FieldType,
                FieldTypeText = GetFieldTypeDisplayName(Enum.Parse<DocumentFieldType>(f.FieldType)),
                Description = f.Description,
                ExtractionPrompt = f.ExtractionPrompt,
                IsRequired = f.IsRequired,
                ValidationRegex = f.ValidationRegex,
                DefaultValue = f.DefaultValue,
                FieldOrder = f.FieldOrder,
                Status = f.Status,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            }).ToList() ?? [],
            DocumentCount = documentType.Documents?.Count ?? 0
        };
    }

    private static string GetFieldTypeDisplayName(DocumentFieldType fieldType)
    {
        return fieldType switch
        {
            DocumentFieldType.TEXT => "Texto",
            DocumentFieldType.LONG_TEXT => "Texto Longo",
            DocumentFieldType.NUMBER => "Número",
            DocumentFieldType.DECIMAL => "Decimal",
            DocumentFieldType.CURRENCY => "Moeda",
            DocumentFieldType.DATE => "Data",
            DocumentFieldType.DATETIME => "Data e Hora",
            DocumentFieldType.EMAIL => "Email",
            DocumentFieldType.PHONE => "Telefone",
            DocumentFieldType.CNPJ => "CNPJ",
            DocumentFieldType.CPF => "CPF",
            DocumentFieldType.URL => "URL",
            DocumentFieldType.BOOLEAN => "Sim/Não",
            _ => fieldType.ToString()
        };
    }

    #endregion
}