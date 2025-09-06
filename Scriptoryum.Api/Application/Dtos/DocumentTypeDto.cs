using Scriptoryum.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Scriptoryum.Api.Application.Dtos;

/// <summary>
/// DTO para exibição de tipos de documentos
/// </summary>
public class DocumentTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public bool IsSystemDefault { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DocumentTypeFieldDto> Fields { get; set; } = [];
    public int DocumentCount { get; set; }
}

/// <summary>
/// DTO para criação de tipos de documentos
/// </summary>
public class CreateDocumentTypeDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public List<CreateDocumentTypeFieldDto> Fields { get; set; } = [];
}

/// <summary>
/// DTO para atualização de tipos de documentos
/// </summary>
public class UpdateDocumentTypeDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(20)]
    public string Status { get; set; } = "Active";
    
    public List<UpdateDocumentTypeFieldDto> Fields { get; set; } = [];
}

/// <summary>
/// DTO para campos de tipos de documentos
/// </summary>
public class DocumentTypeFieldDto
{
    public int Id { get; set; }
    public int DocumentTypeId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string FieldTypeText { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExtractionPrompt { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationRegex { get; set; }
    public string? DefaultValue { get; set; }
    public int FieldOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO para criação de campos de tipos de documentos
/// </summary>
public class CreateDocumentTypeFieldDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FieldName { get; set; } = string.Empty;
    
    [Required]
    public string FieldType { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; }
    
    public string ExtractionPrompt { get; set; }
    
    public bool IsRequired { get; set; } = false;
    
    [StringLength(500)]
    public string ValidationRegex { get; set; }
    
    [StringLength(500)]
    public string DefaultValue { get; set; }
    
    public int FieldOrder { get; set; } = 1;
}

/// <summary>
/// DTO para atualização de campos de tipos de documentos
/// </summary>
public class UpdateDocumentTypeFieldDto
{
    public int? Id { get; set; } // null para novos campos
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FieldName { get; set; } = string.Empty;
    
    [Required]
    public string FieldType { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public string? ExtractionPrompt { get; set; }
    
    public bool IsRequired { get; set; } = false;
    
    [StringLength(500)]
    public string? ValidationRegex { get; set; }
    
    [StringLength(500)]
    public string? DefaultValue { get; set; }
    
    public int FieldOrder { get; set; } = 1;
    
    [StringLength(20)]
    public string Status { get; set; } = "Active";
    
    public bool IsDeleted { get; set; } = false; // Para marcar campos para exclusão
}

/// <summary>
/// DTO para templates de tipos de documentos
/// </summary>
public class DocumentTypeTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsPublic { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public string? CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DocumentTypeFieldTemplateDto> Fields { get; set; } = [];
}

/// <summary>
/// DTO para campos de templates
/// </summary>
public class DocumentTypeFieldTemplateDto
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExtractionPrompt { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationRegex { get; set; }
    public string? DefaultValue { get; set; }
    public int FieldOrder { get; set; }
}

/// <summary>
/// DTO para criação de templates
/// </summary>
public class CreateDocumentTypeTemplateDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(50)]
    public string? Category { get; set; }
    
    public bool IsPublic { get; set; } = true;
    
    public List<CreateDocumentTypeFieldDto> Fields { get; set; } = [];
}

/// <summary>
/// DTO para aplicar template a um tipo de documento
/// </summary>
public class ApplyTemplateDto
{
    [Required]
    public int TemplateId { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DocumentTypeName { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? DocumentTypeDescription { get; set; }
    
    public bool OverwriteExistingFields { get; set; } = false;
}

/// <summary>
/// DTO para associar documento a tipo
/// </summary>
public class AssociateDocumentTypeDto
{
    [Required]
    public int DocumentId { get; set; }
    
    [Required]
    public int DocumentTypeId { get; set; }
    
    public bool TriggerExtraction { get; set; } = true;
}

/// <summary>
/// DTO para valores de campos extraídos
/// </summary>
public class DocumentFieldValueDto
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int DocumentTypeFieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string? ExtractedValue { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string? ContextExcerpt { get; set; }
    public int? StartPosition { get; set; }
    public int? EndPosition { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public string? ValidatedByUserId { get; set; }
    public string ValidatedByUserName { get; set; } = string.Empty;
    public DateTime? ValidatedAt { get; set; }
    public string? CorrectedValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO para validação/correção de valores de campos
/// </summary>
public class ValidateFieldValueDto
{
    [Required]
    public int FieldValueId { get; set; }
    
    [Required]
    public ValidationStatus ValidationStatus { get; set; }
    
    public string? CorrectedValue { get; set; }
    
    [StringLength(500)]
    public string? ValidationNotes { get; set; }
    
    // Propriedades adicionais para validação de documentos
    public int DocumentId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}

/// <summary>
/// DTO para estatísticas de uso de tipos de documentos
/// </summary>
public class DocumentTypeUsageStatsDto
{
    public int DocumentTypeId { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public DateTime? LastUsed { get; set; }
}

/// <summary>
/// DTO para resposta de operações
/// </summary>
public class DocumentTypeOperationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? DocumentTypeId { get; set; }
    public int? DocumentId { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}