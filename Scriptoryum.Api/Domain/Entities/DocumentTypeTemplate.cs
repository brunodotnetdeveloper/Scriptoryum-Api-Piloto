using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Scriptoryum.Api.Domain.Entities;

public class DocumentTypeTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public string TemplateData { get; set; } = string.Empty;

    public bool IsPublic { get; set; } = true;

    [Required]
    public int OrganizationId { get; set; }

    public int UsageCount { get; set; } = 0;

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("CreatedByUserId")]
    public virtual ApplicationUser CreatedByUser { get; set; }

    // Helper methods for template data
    public DocumentTypeTemplateData? GetTemplateData()
    {
        if (string.IsNullOrEmpty(TemplateData))
            return null;

        try
        {
            return JsonSerializer.Deserialize<DocumentTypeTemplateData>(TemplateData);
        }
        catch
        {
            return null;
        }
    }

    public void SetTemplateData(DocumentTypeTemplateData templateData)
    {
        TemplateData = JsonSerializer.Serialize(templateData);
    }
}
