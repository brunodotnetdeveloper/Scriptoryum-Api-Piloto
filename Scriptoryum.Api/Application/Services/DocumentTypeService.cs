#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Application.Interfaces;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;
using Scriptoryum.Api.Infrastructure.Context;
using System.Text.Json;

namespace Scriptoryum.Api.Application.Services;

/// <summary>
/// Serviço para gestão de tipos de documentos
/// </summary>
public class DocumentTypeService : IDocumentTypeService
{
    private readonly ScriptoryumDbContext _context;
    private readonly ILogger<DocumentTypeService> _logger;

    public DocumentTypeService(ScriptoryumDbContext context, ILogger<DocumentTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region CRUD Básico

    public async Task<DocumentTypeDto?> GetByIdAsync(int id, int organizationId)
    {
        var documentType = await _context.DocumentTypes
            .Include(dt => dt.Fields.Where(f => f.Status == "Active"))
            .Include(dt => dt.Organization)
            .Where(dt => dt.Id == id && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
            return null;

        return MapToDto(documentType);
    }

    public async Task<IEnumerable<DocumentTypeDto>> GetAllByOrganizationAsync(int organizationId, bool includeInactive = false)
    {
        var query = _context.DocumentTypes
            .Include(dt => dt.Fields.Where(f => f.Status == "Active"))
            .Include(dt => dt.Organization)
            .Where(dt => dt.OrganizationId == organizationId);

        if (!includeInactive)
        {
            query = query.Where(dt => dt.Status == "Active");
        }

        var documentTypes = await query
            .OrderBy(dt => dt.Name)
            .ToListAsync();

        return documentTypes.Select(MapToDto);
    }

    public async Task<DocumentTypeDto> CreateAsync(CreateDocumentTypeDto dto, int organizationId, string userId)
    {
        var documentType = new DocumentType
        {
            Name = dto.Name,
            Description = dto.Description,
            OrganizationId = organizationId,
            IsSystemDefault = false,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentTypes.Add(documentType);
        await _context.SaveChangesAsync();

        // Adicionar campos se fornecidos
        if (dto.Fields?.Any() == true)
        {
            foreach (var fieldDto in dto.Fields)
            {
                var field = new DocumentTypeField
                {
                    DocumentTypeId = documentType.Id,
                    FieldName = fieldDto.FieldName,
                    FieldType = fieldDto.FieldType.ToString(),
                    Description = fieldDto.Description,
                    ExtractionPrompt = fieldDto.ExtractionPrompt,
                    IsRequired = fieldDto.IsRequired,
                    ValidationRegex = fieldDto.ValidationRegex,
                    DefaultValue = fieldDto.DefaultValue,
                    FieldOrder = fieldDto.FieldOrder,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DocumentTypeFields.Add(field);
            }

            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Tipo de documento '{Name}' criado com sucesso por usuário {UserId}", dto.Name, userId);

        return await GetByIdAsync(documentType.Id, organizationId) ?? throw new InvalidOperationException("Erro ao recuperar tipo de documento criado");
    }

    public async Task<DocumentTypeDto> UpdateAsync(int id, UpdateDocumentTypeDto dto, int organizationId, string userId)
    {
        var documentType = await _context.DocumentTypes
            .Include(dt => dt.Fields)
            .Where(dt => dt.Id == id && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
            throw new InvalidOperationException("Tipo de documento não encontrado");

        documentType.Name = dto.Name;
        documentType.Description = dto.Description;
        documentType.UpdatedAt = DateTime.UtcNow;
        documentType.Status = dto.Status;

        // Atualizar campos se fornecidos
        if (dto.Fields?.Any() == true)
        {
            await UpdateDocumentTypeFieldsAsync(documentType, dto.Fields, userId);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tipo de documento '{Name}' (ID: {Id}) atualizado por usuário {UserId}", dto.Name, id, userId);

        return await GetByIdAsync(id, organizationId) ?? throw new InvalidOperationException("Erro ao recuperar tipo de documento atualizado");
    }

    public async Task<bool> DeleteAsync(int id, int organizationId, string userId)
    {
        var documentType = await _context.DocumentTypes
            .Where(dt => dt.Id == id && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
            return false;

        // Verificar se há documentos associados
        var hasDocuments = await _context.Documents
            .AnyAsync(d => d.DocumentTypeId == id);

        if (hasDocuments)
        {
            throw new InvalidOperationException("Não é possível excluir um tipo de documento que possui documentos associados");
        }

        _context.DocumentTypes.Remove(documentType);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tipo de documento '{Name}' (ID: {Id}) excluído por usuário {UserId}", documentType.Name, id, userId);

        return true;
    }

    public async Task<bool> ActivateAsync(int id, int organizationId, string userId)
    {
        return await UpdateStatusAsync(id, organizationId, "Active", userId);
    }

    public async Task<bool> DeactivateAsync(int id, int organizationId, string userId)
    {
        return await UpdateStatusAsync(id, organizationId, "Inactive", userId);
    }

    #endregion

    #region Gestão de Campos

    public async Task<DocumentTypeFieldDto> AddFieldAsync(int documentTypeId, CreateDocumentTypeFieldDto dto, int organizationId, string userId)
    {
        var documentType = await _context.DocumentTypes
            .Where(dt => dt.Id == documentTypeId && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
            throw new InvalidOperationException("Tipo de documento não encontrado");

        var field = new DocumentTypeField
        {
            DocumentTypeId = documentTypeId,
            FieldName = dto.FieldName,
            FieldType = dto.FieldType.ToString(),
            Description = dto.Description,
            ExtractionPrompt = dto.ExtractionPrompt,
            IsRequired = dto.IsRequired,
            ValidationRegex = dto.ValidationRegex,
            DefaultValue = dto.DefaultValue,
            FieldOrder = dto.FieldOrder,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentTypeFields.Add(field);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Campo '{FieldName}' adicionado ao tipo de documento {DocumentTypeId} por usuário {UserId}", dto.FieldName, documentTypeId, userId);

        return MapFieldToDto(field);
    }

    public async Task<DocumentTypeFieldDto> UpdateFieldAsync(int fieldId, UpdateDocumentTypeFieldDto dto, int organizationId, string userId)
    {
        var field = await _context.DocumentTypeFields
            .Include(f => f.DocumentType)
            .Where(f => f.Id == fieldId && f.DocumentType.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (field == null)
            throw new InvalidOperationException("Campo não encontrado");

        field.FieldName = dto.FieldName;
        field.FieldType = dto.FieldType.ToString();
        field.Description = dto.Description;
        field.ExtractionPrompt = dto.ExtractionPrompt;
        field.IsRequired = dto.IsRequired;
        field.ValidationRegex = dto.ValidationRegex;
        field.DefaultValue = dto.DefaultValue;
        field.FieldOrder = dto.FieldOrder;
        field.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Campo '{FieldName}' (ID: {FieldId}) atualizado por usuário {UserId}", dto.FieldName, fieldId, userId);

        return MapFieldToDto(field);
    }

    public async Task<bool> RemoveFieldAsync(int fieldId, int organizationId, string userId)
    {
        var field = await _context.DocumentTypeFields
            .Include(f => f.DocumentType)
            .Where(f => f.Id == fieldId && f.DocumentType.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (field == null)
            return false;

        // Verificar se há valores de campo associados
        var hasValues = await _context.DocumentFieldValues
            .AnyAsync(v => v.DocumentTypeFieldId == fieldId);

        if (hasValues)
        {
            // Marcar como inativo em vez de excluir
            field.Status = "Inactive";
            field.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.DocumentTypeFields.Remove(field);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Campo '{FieldName}' (ID: {FieldId}) removido por usuário {UserId}", field.FieldName, fieldId, userId);

        return true;
    }

    public async Task<bool> ReorderFieldsAsync(int documentTypeId, Dictionary<int, int> fieldOrders, int organizationId, string userId)
    {
        var documentType = await _context.DocumentTypes
            .Include(dt => dt.Fields)
            .Where(dt => dt.Id == documentTypeId && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
            return false;

        foreach (var fieldOrder in fieldOrders)
        {
            var field = documentType.Fields.FirstOrDefault(f => f.Id == fieldOrder.Key);
            if (field != null)
            {
                field.FieldOrder = fieldOrder.Value;
                field.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ordem dos campos do tipo de documento {DocumentTypeId} reordenada por usuário {UserId}", documentTypeId, userId);

        return true;
    }

    #endregion

    #region Templates

    public async Task<IEnumerable<DocumentTypeTemplateDto>> GetTemplatesAsync(int organizationId, bool includePublic = true)
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

        return templates.Select(MapTemplateToDto);
    }

    public async Task<DocumentTypeTemplateDto?> GetTemplateByIdAsync(int templateId, int organizationId)
    {
        var template = await _context.DocumentTypeTemplates
            .Include(t => t.Organization)
            .Where(t => t.Id == templateId && (t.OrganizationId == organizationId || t.IsPublic))
            .FirstOrDefaultAsync();

        return template != null ? MapTemplateToDto(template) : null;
    }

    public async Task<DocumentTypeTemplateDto> CreateTemplateAsync(CreateDocumentTypeTemplateDto dto, int organizationId, string userId)
    {
        var templateData = new DocumentTypeTemplateData
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Fields = dto.Fields.Select(f => new DocumentTypeFieldTemplate
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

        _logger.LogInformation("Template '{Name}' criado com sucesso por usuário {UserId}", dto.Name, userId);

        return MapTemplateToDto(template);
    }

    public async Task<DocumentTypeDto> ApplyTemplateAsync(ApplyTemplateDto dto, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == dto.TemplateId && (t.OrganizationId == organizationId || t.IsPublic))
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

        var createDto = new CreateDocumentTypeDto
        {
            Name = dto.DocumentTypeName,
            Description = dto.DocumentTypeDescription ?? templateData.Description,
            Fields = templateData.Fields.Select(f => new CreateDocumentTypeFieldDto
            {
                FieldName = f.FieldName,
                FieldType = f.FieldType,
                Description = f.Description,
                ExtractionPrompt = f.ExtractionPrompt,
                IsRequired = f.IsRequired,
                ValidationRegex = f.ValidationRegex,
                DefaultValue = f.DefaultValue,
                FieldOrder = f.FieldOrder
            }).ToList()
        };

        return await CreateAsync(createDto, organizationId, userId);
    }

    public async Task<bool> DeleteTemplateAsync(int templateId, int organizationId, string userId)
    {
        var template = await _context.DocumentTypeTemplates
            .Where(t => t.Id == templateId && t.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (template == null)
            return false;

        _context.DocumentTypeTemplates.Remove(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Template '{Name}' (ID: {TemplateId}) excluído por usuário {UserId}", 
            template.Name, templateId, userId);

        return true;
    }

    #endregion

    #region Associação de Documentos

    public async Task<DocumentTypeOperationResponseDto> AssociateDocumentAsync(AssociateDocumentTypeDto dto, int organizationId, string userId)
    {
        var document = await _context.Documents
            .Include(d => d.Workspace)
            .Where(d => d.Id == dto.DocumentId && d.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (document == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Documento não encontrado"
            };
        }

        var documentType = await _context.DocumentTypes
            .Where(dt => dt.Id == dto.DocumentTypeId && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Tipo de documento não encontrado"
            };
        }

        document.DocumentTypeId = dto.DocumentTypeId;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Documento {DocumentId} associado ao tipo {DocumentTypeId} por usuário {UserId}", 
            dto.DocumentId, dto.DocumentTypeId, userId);

        return new DocumentTypeOperationResponseDto
        {
            Success = true,
            Message = "Documento associado com sucesso",
            DocumentId = document.Id,
            DocumentTypeId = documentType.Id
        };
    }

    public async Task<DocumentTypeOperationResponseDto> ChangeDocumentTypeAsync(int documentId, int newDocumentTypeId, int organizationId, string userId)
    {
        var document = await _context.Documents
            .Include(d => d.Workspace)
            .Where(d => d.Id == documentId && d.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (document == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Documento não encontrado"
            };
        }

        var newDocumentType = await _context.DocumentTypes
            .Where(dt => dt.Id == newDocumentTypeId && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (newDocumentType == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Novo tipo de documento não encontrado"
            };
        }

        var oldDocumentTypeId = document.DocumentTypeId;
        document.DocumentTypeId = newDocumentTypeId;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tipo do documento {DocumentId} alterado de {OldTypeId} para {NewTypeId} por usuário {UserId}", 
            documentId, oldDocumentTypeId, newDocumentTypeId, userId);

        return new DocumentTypeOperationResponseDto
        {
            Success = true,
            Message = "Tipo de documento alterado com sucesso",
            DocumentId = document.Id,
            DocumentTypeId = newDocumentType.Id
        };
    }

    public async Task<DocumentTypeOperationResponseDto> RemoveDocumentAssociationAsync(int documentId, int organizationId, string userId)
    {
        var document = await _context.Documents
            .Include(d => d.Workspace)
            .Where(d => d.Id == documentId && d.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (document == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Documento não encontrado"
            };
        }

        document.DocumentTypeId = null;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Associação de tipo removida do documento {DocumentId} por usuário {UserId}", 
            documentId, userId);

        return new DocumentTypeOperationResponseDto
        {
            Success = true,
            Message = "Associação de tipo removida com sucesso",
            DocumentId = document.Id
        };
    }

    #endregion

    #region Extração e Validação de Campos

    public async Task<IEnumerable<DocumentFieldValueDto>> GetDocumentFieldValuesAsync(int documentId, int organizationId)
    {
        var fieldValues = await _context.DocumentFieldValues
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Include(fv => fv.DocumentTypeField)
            .Where(fv => fv.DocumentId == documentId && fv.Document.Workspace.OrganizationId == organizationId)
            .OrderBy(fv => fv.DocumentTypeField.FieldOrder)
            .ToListAsync();

        return fieldValues.Select(MapFieldValueToDto);
    }

    public async Task<DocumentTypeOperationResponseDto> TriggerFieldExtractionAsync(int documentId, int organizationId, string userId)
    {
        var document = await _context.Documents
            .Include(d => d.Workspace)
            .Include(d => d.DocumentType)
            .ThenInclude(dt => dt!.Fields.Where(f => f.Status == "Active"))
            .Where(d => d.Id == documentId && d.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (document == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Documento não encontrado"
            };
        }

        if (document.DocumentType == null)
        {
            return new DocumentTypeOperationResponseDto
            {
                Success = false,
                Message = "Documento não possui tipo associado"
            };
        }

        // TODO: Implementar lógica de extração de campos usando IA
        // Por enquanto, apenas registra a solicitação
        _logger.LogInformation("Extração de campos solicitada para documento {DocumentId} por usuário {UserId}", 
            documentId, userId);

        return new DocumentTypeOperationResponseDto
        {
            Success = true,
            Message = "Extração de campos iniciada",
            DocumentId = document.Id,
            DocumentTypeId = document.DocumentType.Id
        };
    }

    public async Task<DocumentFieldValueDto> ValidateFieldValueAsync(ValidateFieldValueDto dto, int organizationId, string userId)
    {
        var fieldValue = await _context.DocumentFieldValues
            .Include(fv => fv.Document)
            .ThenInclude(d => d.Workspace)
            .Include(fv => fv.DocumentTypeField)
            .Where(fv => fv.Id == dto.FieldValueId && fv.Document.Workspace.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (fieldValue == null)
            throw new InvalidOperationException("Valor de campo não encontrado");

        fieldValue.CorrectedValue = dto.CorrectedValue;
        fieldValue.ValidationStatus = dto.ValidationStatus.ToString();
        fieldValue.ValidatedByUserId = userId;
        fieldValue.ValidatedAt = DateTime.UtcNow;
        fieldValue.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Valor do campo {FieldName} validado para documento {DocumentId} por usuário {UserId}", 
            fieldValue.DocumentTypeField.FieldName, fieldValue.DocumentId, userId);

        return MapFieldValueToDto(fieldValue);
    }

    public async Task<IEnumerable<DocumentFieldValueDto>> ValidateMultipleFieldValuesAsync(IEnumerable<ValidateFieldValueDto> dtos, int organizationId, string userId)
    {
        var results = new List<DocumentFieldValueDto>();

        foreach (var dto in dtos)
        {
            try
            {
                var result = await ValidateFieldValueAsync(dto, organizationId, userId);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar campo {FieldValueId}", dto.FieldValueId);
            }
        }

        return results;
    }

    public async Task<DocumentFieldValueDto> ValidateDocumentFieldAsync(int documentId, string fieldName, string value, int organizationId, string userId)
    {
        var dto = new ValidateFieldValueDto
        {
            DocumentId = documentId,
            FieldName = fieldName,
            Value = value
        };
        
        return await ValidateFieldValueAsync(dto, organizationId, userId);
    }

    #endregion

    #region Consultas e Relatórios

    public async Task<IEnumerable<DocumentTypeDto>> SearchDocumentTypesAsync(string searchTerm, int organizationId)
    {
        var documentTypes = await _context.DocumentTypes
            .Include(dt => dt.Fields.Where(f => f.Status == "Active"))
            .Include(dt => dt.Organization)
            .Where(dt => dt.OrganizationId == organizationId && 
                        (dt.Name.Contains(searchTerm) || 
                         dt.Description != null && dt.Description.Contains(searchTerm)))
            .OrderBy(dt => dt.Name)
            .ToListAsync();

        return documentTypes.Select(MapToDto);
    }

    public async Task<IEnumerable<DocumentTypeUsageStatsDto>> GetDocumentTypeUsageStatsAsync(int organizationId)
    {
        var stats = await _context.DocumentTypes
            .Where(dt => dt.OrganizationId == organizationId)
            .Select(dt => new DocumentTypeUsageStatsDto
            {
                DocumentTypeId = dt.Id,
                DocumentTypeName = dt.Name,
                DocumentCount = dt.Documents!.Count(),
                LastUsed = dt.Documents!.Any() ? dt.Documents!.Max(d => d.CreatedAt).DateTime : (DateTime?)null
            })
            .OrderByDescending(s => s.DocumentCount)
            .ToListAsync();

        return stats;
    }

    public async Task<IEnumerable<DocumentTypeDto>> GetMostUsedDocumentTypesAsync(int organizationId, int limit = 10)
    {
        var documentTypeIds = await _context.Documents
            .Include(d => d.Workspace)
            .Where(d => d.Workspace.OrganizationId == organizationId && d.DocumentTypeId != null)
            .GroupBy(d => d.DocumentTypeId)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.Key!.Value)
            .ToListAsync();

        var documentTypes = await _context.DocumentTypes
            .Include(dt => dt.Fields.Where(f => f.Status == "Active"))
            .Include(dt => dt.Organization)
            .Where(dt => documentTypeIds.Contains(dt.Id))
            .ToListAsync();

        return documentTypes.Select(MapToDto);
    }

    public async Task<Dictionary<string, object>> GetDocumentTypeStatisticsAsync(int documentTypeId, int organizationId)
    {
        var documentType = await _context.DocumentTypes
            .Include(dt => dt.Documents)
            .Include(dt => dt.Fields)
            .Where(dt => dt.Id == documentTypeId && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
            return new Dictionary<string, object>();

        var totalDocuments = documentType.Documents?.Count ?? 0;
        var activeFields = documentType.Fields?.Count(f => f.Status == "Active") ?? 0;
        var lastUsed = documentType.Documents?.Any() == true ? documentType.Documents.Max(d => d.CreatedAt).DateTime : (DateTime?)null;

        return new Dictionary<string, object>
        {
            ["DocumentTypeId"] = documentType.Id,
            ["DocumentTypeName"] = documentType.Name,
            ["TotalDocuments"] = totalDocuments,
            ["ActiveFields"] = activeFields,
            ["LastUsed"] = lastUsed,
            ["CreatedAt"] = documentType.CreatedAt,
            ["UpdatedAt"] = documentType.UpdatedAt,
            ["Status"] = documentType.Status
        };
    }

    public async Task<bool> CanDeleteDocumentTypeAsync(int documentTypeId, int organizationId)
    {
        var hasDocuments = await _context.Documents
            .AnyAsync(d => d.DocumentTypeId == documentTypeId && d.Workspace.OrganizationId == organizationId);

        return !hasDocuments;
    }

    public async Task<bool> DocumentTypeExistsAsync(int documentTypeId, int organizationId)
    {
        return await _context.DocumentTypes
            .AnyAsync(dt => dt.Id == documentTypeId && dt.OrganizationId == organizationId);
    }

    public async Task<bool> DocumentTypeNameExistsAsync(string name, int organizationId, int? excludeId = null)
    {
        var query = _context.DocumentTypes
            .Where(dt => dt.Name == name && dt.OrganizationId == organizationId);

        if (excludeId.HasValue)
        {
            query = query.Where(dt => dt.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> FieldNameExistsInDocumentTypeAsync(string fieldName, int documentTypeId, int? excludeFieldId = null)
    {
        var query = _context.DocumentTypeFields
            .Where(f => f.FieldName == fieldName && f.DocumentTypeId == documentTypeId);

        if (excludeFieldId.HasValue)
        {
            query = query.Where(f => f.Id != excludeFieldId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<ValidationResult> ValidateDocumentTypeAsync(CreateDocumentTypeDto dto, int organizationId)
    {
        var result = new ValidationResult { IsValid = true };

        // Validar nome
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            result.Errors.Add("Nome é obrigatório");
            result.IsValid = false;
        }
        else if (await DocumentTypeNameExistsAsync(dto.Name, organizationId))
        {
            result.Errors.Add($"Já existe um tipo de documento com o nome '{dto.Name}'");
            result.IsValid = false;
        }

        // Validar campos
        if (dto.Fields?.Any() == true)
        {
            var fieldNames = new HashSet<string>();
            foreach (var field in dto.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.FieldName))
                {
                    result.Errors.Add("Nome do campo é obrigatório");
                    result.IsValid = false;
                }
                else if (!fieldNames.Add(field.FieldName.ToLower()))
                {
                    result.Errors.Add($"Campo '{field.FieldName}' está duplicado");
                    result.IsValid = false;
                }
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateDocumentTypeUpdateAsync(int documentTypeId, UpdateDocumentTypeDto dto, int organizationId)
    {
        var result = new ValidationResult { IsValid = true };

        // Verificar se o tipo de documento existe
        if (!await DocumentTypeExistsAsync(documentTypeId, organizationId))
        {
            result.Errors.Add("Tipo de documento não encontrado");
            result.IsValid = false;
            return result;
        }

        // Validar nome
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            result.Errors.Add("Nome é obrigatório");
            result.IsValid = false;
        }
        else if (await DocumentTypeNameExistsAsync(dto.Name, organizationId, documentTypeId))
        {
            result.Errors.Add($"Já existe um tipo de documento com o nome '{dto.Name}'");
            result.IsValid = false;
        }

        // Validar campos
        if (dto.Fields?.Any() == true)
        {
            var fieldNames = new HashSet<string>();
            foreach (var field in dto.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.FieldName))
                {
                    result.Errors.Add("Nome do campo é obrigatório");
                    result.IsValid = false;
                }
                else if (!fieldNames.Add(field.FieldName.ToLower()))
                {
                    result.Errors.Add($"Campo '{field.FieldName}' está duplicado");
                    result.IsValid = false;
                }
                else if (field.Id == 0 && await FieldNameExistsInDocumentTypeAsync(field.FieldName, documentTypeId))
                {
                    result.Errors.Add($"Campo '{field.FieldName}' já existe neste tipo de documento");
                    result.IsValid = false;
                }
                else if (field.Id > 0 && await FieldNameExistsInDocumentTypeAsync(field.FieldName, documentTypeId, field.Id))
                {
                    result.Errors.Add($"Campo '{field.FieldName}' já existe neste tipo de documento");
                    result.IsValid = false;
                }
            }
        }

        return result;
    }

    #endregion

    #region Métodos Privados

    private async Task<bool> UpdateStatusAsync(int id, int organizationId, string status, string userId)
    {
        var documentType = await _context.DocumentTypes
            .Where(dt => dt.Id == id && dt.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (documentType == null)
            return false;

        documentType.Status = status;
        documentType.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Status do tipo de documento '{Name}' (ID: {Id}) alterado para {Status} por usuário {UserId}", 
            documentType.Name, id, status, userId);

        return true;
    }

    private async Task UpdateDocumentTypeFieldsAsync(DocumentType documentType, List<UpdateDocumentTypeFieldDto> fieldDtos, string userId)
    {
        foreach (var fieldDto in fieldDtos)
        {
            var existingField = documentType.Fields.FirstOrDefault(f => f.Id == fieldDto.Id);
            if (existingField != null)
            {
                existingField.FieldName = fieldDto.FieldName;
                existingField.FieldType = fieldDto.FieldType.ToString();
                existingField.Description = fieldDto.Description;
                existingField.ExtractionPrompt = fieldDto.ExtractionPrompt;
                existingField.IsRequired = fieldDto.IsRequired;
                existingField.ValidationRegex = fieldDto.ValidationRegex;
                existingField.DefaultValue = fieldDto.DefaultValue;
                existingField.FieldOrder = fieldDto.FieldOrder;
                existingField.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    #endregion

    #region Mapeamento

    private static DocumentTypeDto MapToDto(DocumentType documentType)
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
            Fields = documentType.Fields?.Select(MapFieldToDto).ToList() ?? [],
            DocumentCount = documentType.Documents?.Count ?? 0
        };
    }

    private static DocumentTypeFieldDto MapFieldToDto(DocumentTypeField field)
    {
        return new DocumentTypeFieldDto
        {
            Id = field.Id,
            DocumentTypeId = field.DocumentTypeId,
            FieldName = field.FieldName,
            FieldType = field.FieldType,
            FieldTypeText = GetFieldTypeDisplayName(Enum.Parse<DocumentFieldType>(field.FieldType)),
            Description = field.Description,
            ExtractionPrompt = field.ExtractionPrompt,
            IsRequired = field.IsRequired,
            ValidationRegex = field.ValidationRegex,
            DefaultValue = field.DefaultValue,
            FieldOrder = field.FieldOrder,
            Status = field.Status,
            CreatedAt = field.CreatedAt,
            UpdatedAt = field.UpdatedAt
        };
    }

    private static DocumentTypeTemplateDto MapTemplateToDto(DocumentTypeTemplate template)
    {
        var templateData = JsonSerializer.Deserialize<DocumentTypeTemplateData>(template.TemplateData);
        
        return new DocumentTypeTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = templateData?.Category,
            IsPublic = template.IsPublic,
            OrganizationId = template.OrganizationId,
            OrganizationName = template.Organization?.Name ?? string.Empty,
            UsageCount = 0, // TODO: Implementar contagem de uso
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

    private static DocumentFieldValueDto MapFieldValueToDto(DocumentFieldValue fieldValue)
    {
        return new DocumentFieldValueDto
        {
            Id = fieldValue.Id,
            DocumentId = fieldValue.DocumentId,
            DocumentTypeFieldId = fieldValue.DocumentTypeFieldId,
            FieldName = fieldValue.DocumentTypeField?.FieldName ?? string.Empty,
            FieldType = fieldValue.DocumentTypeField?.FieldType ?? string.Empty,
            ExtractedValue = fieldValue.ExtractedValue,
            ConfidenceScore = fieldValue.ConfidenceScore,
            ContextExcerpt = fieldValue.ContextExcerpt,
            StartPosition = fieldValue.StartPosition,
            EndPosition = fieldValue.EndPosition,
            ValidationStatus = fieldValue.ValidationStatus.ToString(),
            ValidatedByUserId = fieldValue.ValidatedByUserId,
            ValidatedByUserName = string.Empty, // TODO: Buscar nome do usuário
            ValidatedAt = fieldValue.ValidatedAt,
            CorrectedValue = fieldValue.CorrectedValue,
            CreatedAt = fieldValue.CreatedAt,
            UpdatedAt = fieldValue.UpdatedAt
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