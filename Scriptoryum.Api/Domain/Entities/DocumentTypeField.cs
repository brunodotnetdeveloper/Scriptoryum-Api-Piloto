using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scriptoryum.Api.Domain.Entities
{
    public class DocumentTypeField
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string FieldType { get; set; } = string.Empty; // TEXT, NUMBER, DATE, EMAIL, PHONE, CURRENCY, etc.

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "text")]
        public string? ExtractionPrompt { get; set; }

        public bool IsRequired { get; set; } = false;

        [StringLength(500)]
        public string? ValidationRegex { get; set; }

        [StringLength(500)]
        public string? DefaultValue { get; set; }

        public int FieldOrder { get; set; } = 1;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DocumentTypeId")]
        public virtual DocumentType DocumentType { get; set; } = null!;

        public virtual ICollection<DocumentFieldValue> FieldValues { get; set; } = new List<DocumentFieldValue>();
    }
}
