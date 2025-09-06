using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Scriptoryum.Api.Domain.Entities;

public class DocumentFieldValue
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int DocumentTypeFieldId { get; set; }

    [Column(TypeName = "text")]
    public string? ExtractedValue { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? ConfidenceScore { get; set; }

    [StringLength(2000)]
    public string? ContextExcerpt { get; set; }

    public int? StartPosition { get; set; }

    public int? EndPosition { get; set; }

    [Required]
    [StringLength(20)]
    public string ValidationStatus { get; set; } = "Pending"; // Pending, Valid, Invalid, ManuallyReviewed

    [StringLength(450)] // ASP.NET Identity default Id length
    public string? ValidatedByUserId { get; set; }

    public DateTime? ValidatedAt { get; set; }

    [Column(TypeName = "text")]
    public string? CorrectedValue { get; set; }

    [Column(TypeName = "jsonb")]
    public string? ExtractionMetadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("DocumentTypeFieldId")]
    public virtual DocumentTypeField DocumentTypeField { get; set; } = null!;

    [ForeignKey("ValidatedByUserId")]
    public virtual ApplicationUser ValidatedByUser { get; set; }

    public virtual ICollection<DocumentFieldValueHistory> History { get; set; } = new List<DocumentFieldValueHistory>();

    // Helper methods for metadata
    public T? GetExtractionMetadata<T>() where T : class
    {
        if (string.IsNullOrEmpty(ExtractionMetadata))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(ExtractionMetadata);
        }
        catch
        {
            return null;
        }
    }

    public void SetExtractionMetadata<T>(T metadata) where T : class
    {
        ExtractionMetadata = JsonSerializer.Serialize(metadata);
    }
}
