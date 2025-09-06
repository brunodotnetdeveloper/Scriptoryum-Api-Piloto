using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scriptoryum.Api.Domain.Entities;

public class DocumentFieldValueHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DocumentFieldValueId { get; set; }

    [Column(TypeName = "text")]
    public string PreviousValue { get; set; }

    [Column(TypeName = "text")]
    public string NewValue { get; set; }

    [Required]
    [StringLength(20)]
    public string ChangeType { get; set; } = string.Empty; // AutoExtracted, ManualCorrection, ValidationUpdate

    [StringLength(450)]
    public string ChangedByUserId { get; set; }

    [StringLength(500)]
    public string ChangeReason { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? ConfidenceScoreBefore { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? ConfidenceScoreAfter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("DocumentFieldValueId")]
    public virtual DocumentFieldValue DocumentFieldValue { get; set; } = null!;

    [ForeignKey("ChangedByUserId")]
    public virtual ApplicationUser ChangedByUser { get; set; }    
}
