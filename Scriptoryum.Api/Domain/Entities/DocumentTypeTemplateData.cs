namespace Scriptoryum.Api.Domain.Entities
{
    public class DocumentTypeTemplateData
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public List<DocumentTypeFieldTemplate> Fields { get; set; } = new List<DocumentTypeFieldTemplate>();
    }

    public class DocumentTypeFieldTemplate
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ExtractionPrompt { get; set; }
        public bool IsRequired { get; set; } = false;
        public string? ValidationRegex { get; set; }
        public string? DefaultValue { get; set; }
        public int FieldOrder { get; set; } = 1;
    }
}
