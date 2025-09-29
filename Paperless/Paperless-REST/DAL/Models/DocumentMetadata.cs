using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    [Index(nameof(Title))]
    [Index(nameof(DocumentId), nameof(CreatedAt))]
    public sealed class DocumentMetadata
    {
        // Composite PK (DocumentId + Version)
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid DocumentId { get; set; }
        public int Version { get; set; }

        // Upload info
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }

        // basic metadata
        [MaxLength(512)]
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? LanguageCode { get; set; }

        // EF Core reference
        public User? CreatedByUser { get; set; }
        public DocumentLanguage? DocumentLanguage { get; set; }
        public Document Document { get; set; } = default!;
    }
}
