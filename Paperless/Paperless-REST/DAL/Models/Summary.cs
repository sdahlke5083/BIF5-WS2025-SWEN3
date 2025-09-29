using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    public sealed class Summary
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset CreatedAt { get; set; }
        public string? Model { get; set; }          // gpt-3.5-turbo, gpt-4, etc.
        public Guid LengthPresetId { get; set; }
        public string Content { get; set; } = default!;

        // EF Core reference
        public Document Document { get; set; } = default!;
        public SummaryPreset LengthPreset { get; set; } = default!;
    }

    public sealed class SummaryPreset
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!;        // e.g. "short", "detailed", etc.
        [Required]
        public string Description { get; set; } = default!; // e.g. "A brief summary of the document.", etc.
        [Required] 
        public string Prompt { get; set; } = default!;      // e.g. "Summarize the following document in a concise manner: {document_text}"
        public int MaxTokens { get; set; }                  // e.g. 100, 500, etc.
    }

}
