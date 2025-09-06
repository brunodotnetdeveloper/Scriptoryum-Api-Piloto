using Scriptoryum.Api.Application.Dtos;
using Scriptoryum.Api.Domain.Entities;
using Scriptoryum.Api.Domain.Enums;

namespace Scriptoryum.Api.Application.Interfaces;

/// <summary>
/// Interface para serviços de gestão de tipos de documentos
/// </summary>
public interface IDocumentTypeService
{
    // CRUD básico de tipos de documentos
    Task<DocumentTypeDto?> GetByIdAsync(int id, int organizationId);
    Task<IEnumerable<DocumentTypeDto>> GetAllByOrganizationAsync(int organizationId, bool includeInactive = false);
    Task<DocumentTypeDto> CreateAsync(CreateDocumentTypeDto dto, int organizationId, string userId);
    Task<DocumentTypeDto> UpdateAsync(int id, UpdateDocumentTypeDto dto, int organizationId, string userId);
    Task<bool> DeleteAsync(int id, int organizationId, string userId);
    Task<bool> ActivateAsync(int id, int organizationId, string userId);
    Task<bool> DeactivateAsync(int id, int organizationId, string userId);
    
    // Gestão de campos
    Task<DocumentTypeFieldDto> AddFieldAsync(int documentTypeId, CreateDocumentTypeFieldDto dto, int organizationId, string userId);
    Task<DocumentTypeFieldDto> UpdateFieldAsync(int fieldId, UpdateDocumentTypeFieldDto dto, int organizationId, string userId);
    Task<bool> RemoveFieldAsync(int fieldId, int organizationId, string userId);
    Task<bool> ReorderFieldsAsync(int documentTypeId, Dictionary<int, int> fieldOrders, int organizationId, string userId);
    
    // Templates
    Task<IEnumerable<DocumentTypeTemplateDto>> GetTemplatesAsync(int organizationId, bool includePublic = true);
    Task<DocumentTypeTemplateDto?> GetTemplateByIdAsync(int templateId, int organizationId);
    Task<DocumentTypeTemplateDto> CreateTemplateAsync(CreateDocumentTypeTemplateDto dto, int organizationId, string userId);
    Task<DocumentTypeDto> ApplyTemplateAsync(ApplyTemplateDto dto, int organizationId, string userId);
    Task<bool> DeleteTemplateAsync(int templateId, int organizationId, string userId);
    
    // Associação de documentos
    Task<DocumentTypeOperationResponseDto> AssociateDocumentAsync(AssociateDocumentTypeDto dto, int organizationId, string userId);
    Task<DocumentTypeOperationResponseDto> ChangeDocumentTypeAsync(int documentId, int newDocumentTypeId, int organizationId, string userId);
    Task<DocumentTypeOperationResponseDto> RemoveDocumentAssociationAsync(int documentId, int organizationId, string userId);
    
    // Extração e validação de campos
    Task<IEnumerable<DocumentFieldValueDto>> GetDocumentFieldValuesAsync(int documentId, int organizationId);
    Task<DocumentTypeOperationResponseDto> TriggerFieldExtractionAsync(int documentId, int organizationId, string userId);
    Task<DocumentFieldValueDto> ValidateFieldValueAsync(ValidateFieldValueDto dto, int organizationId, string userId);
    Task<IEnumerable<DocumentFieldValueDto>> ValidateMultipleFieldValuesAsync(IEnumerable<ValidateFieldValueDto> dtos, int organizationId, string userId);
    Task<DocumentFieldValueDto> ValidateDocumentFieldAsync(int documentId, string fieldName, string value, int organizationId, string userId);
    
    // Consultas e relatórios
    Task<IEnumerable<DocumentTypeDto>> SearchDocumentTypesAsync(string searchTerm, int organizationId);
    Task<Dictionary<string, object>> GetDocumentTypeStatisticsAsync(int documentTypeId, int organizationId);
    Task<IEnumerable<DocumentTypeDto>> GetMostUsedDocumentTypesAsync(int organizationId, int limit = 10);
    Task<bool> CanDeleteDocumentTypeAsync(int documentTypeId, int organizationId);
    
    // Validações
    Task<bool> DocumentTypeExistsAsync(int documentTypeId, int organizationId);
    Task<bool> DocumentTypeNameExistsAsync(string name, int organizationId, int? excludeId = null);
    Task<bool> FieldNameExistsInDocumentTypeAsync(string fieldName, int documentTypeId, int? excludeFieldId = null);
    Task<ValidationResult> ValidateDocumentTypeAsync(CreateDocumentTypeDto dto, int organizationId);
    Task<ValidationResult> ValidateDocumentTypeUpdateAsync(int documentTypeId, UpdateDocumentTypeDto dto, int organizationId);
}

/// <summary>
/// Interface para serviços de gestão de campos de documentos
/// </summary>
public interface IDocumentFieldService
{
    // Gestão de valores de campos
    Task<DocumentFieldValueDto?> GetFieldValueByIdAsync(int fieldValueId, int organizationId);
    Task<IEnumerable<DocumentFieldValueDto>> GetFieldValuesByDocumentAsync(int documentId, int organizationId);
    Task<IEnumerable<DocumentFieldValueDto>> GetFieldValuesByFieldAsync(int fieldId, int organizationId);
    
    // Extração automática
    Task<DocumentTypeOperationResponseDto> ExtractFieldValuesAsync(int documentId, int organizationId, string userId);
    Task<DocumentTypeOperationResponseDto> ExtractSpecificFieldAsync(int documentId, int fieldId, int organizationId, string userId);
    Task<DocumentTypeOperationResponseDto> ReextractFieldValuesAsync(int documentId, int organizationId, string userId);
    
    // Validação e correção
    Task<DocumentFieldValueDto> ValidateFieldValueAsync(int fieldValueId, ValidationStatus status, string? correctedValue, int organizationId, string userId);
    Task<IEnumerable<DocumentFieldValueDto>> BulkValidateFieldValuesAsync(Dictionary<int, (ValidationStatus status, string? correctedValue)> validations, int organizationId, string userId);
    
    // Histórico
    Task<IEnumerable<DocumentFieldValueHistoryDto>> GetFieldValueHistoryAsync(int fieldValueId, int organizationId);
    
    // Estatísticas
    Task<Dictionary<string, object>> GetFieldExtractionStatisticsAsync(int fieldId, int organizationId);
    Task<Dictionary<string, object>> GetDocumentExtractionStatisticsAsync(int documentId, int organizationId);
}

/// <summary>
/// Interface para serviços de templates de tipos de documentos
/// </summary>
public interface IDocumentTypeTemplateService
{
    // CRUD de templates
    Task<DocumentTypeTemplateDto?> GetByIdAsync(int templateId, int organizationId);
    Task<IEnumerable<DocumentTypeTemplateDto>> GetAllAsync(int organizationId, bool includePublic = true);
    Task<IEnumerable<DocumentTypeTemplateDto>> GetByCategory(string category, int organizationId);
    Task<DocumentTypeTemplateDto> CreateAsync(CreateDocumentTypeTemplateDto dto, int organizationId, string userId);
    Task<DocumentTypeTemplateDto> UpdateAsync(int templateId, CreateDocumentTypeTemplateDto dto, int organizationId, string userId);
    Task<bool> DeleteAsync(int templateId, int organizationId, string userId);
    
    // Aplicação de templates
    Task<DocumentTypeDto> ApplyToNewDocumentTypeAsync(int templateId, string documentTypeName, string? description, int organizationId, string userId);
    Task<DocumentTypeOperationResponseDto> ApplyToExistingDocumentTypeAsync(int templateId, int documentTypeId, bool overwriteFields, int organizationId, string userId);
    Task<DocumentTypeDto> ApplyTemplateAsync(ApplyTemplateDto dto, int organizationId, string userId);
    Task<DocumentTypeTemplateDto> CreateTemplateFromDocumentTypeAsync(int documentTypeId, string templateName, string? templateDescription, string? category, bool isPublic, int organizationId, string userId);
    
    // Gestão de templates públicos
    Task<IEnumerable<DocumentTypeTemplateDto>> GetPublicTemplatesAsync();
    Task<bool> MakeTemplatePublicAsync(int templateId, int organizationId, string userId);
    Task<bool> MakeTemplatePrivateAsync(int templateId, int organizationId, string userId);
    Task<bool> MakePrivateAsync(int templateId, int organizationId, string userId);
    
    // Estatísticas
    Task<Dictionary<string, object>> GetTemplateUsageStatisticsAsync(int templateId, int organizationId);
    Task<Dictionary<string, object>> GetTemplateStatisticsAsync(int templateId, int organizationId);
    Task<IEnumerable<DocumentTypeTemplateDto>> GetMostUsedTemplatesAsync(int organizationId, int limit = 10);
    
    // Métodos adicionais
    Task<DocumentTypeTemplateDto> DuplicateTemplateAsync(int templateId, string newName, int organizationId, string userId);
    Task<IEnumerable<string>> GetCategoriesAsync(int organizationId);
    Task<IEnumerable<DocumentTypeTemplateDto>> SearchTemplatesAsync(string searchTerm, int organizationId);
}

/// <summary>
/// Resultado de validação
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// DTO para histórico de valores de campos
/// </summary>
public class DocumentFieldValueHistoryDto
{
    public int Id { get; set; }
    public int DocumentFieldValueId { get; set; }
    public string FieldName { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? ChangeReason { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public string ChangedByUserName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Metadata { get; set; }
}