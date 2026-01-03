using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    [Index(nameof(OriginalFileName), nameof(UploadedAt))]
    [Index(nameof(DocumentId), nameof(Version), IsUnique = true)]
    public sealed class FileVersion
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public int Version { get; set; }
        [Required]
        [MaxLength(512)]
        public string OriginalFileName { get; set; } = default!;
        [Required]
        [MaxLength(512)]
        public string StoredName { get; set; } = default!;
        public long SizeBytes { get; set; }
        public Guid FileTypeId { get; set; }
        //[Required]
        //public string ContentSha256 { get; set; } = default!; // TODO: Idee für VirusTotal o.ä.
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public DateTimeOffset UploadedAt { get; set; }
        public Guid? UploadedByUserId { get; set; }


        // EF Core references
        public User? UploadedByUser { get; set; }
        public Document Document { get; set; } = default!;
        public DocumentFileType FileType { get; set; } = default!;
    }

    public sealed class DocumentFileType
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }
        [Required]
        public string DisplayName { get; set; } = default!;     // e.g. "PDF Document"
        [Required]
        public string MimeType { get; set; } = default!;        // e.g. "application/pdf"
        [Required]
        public string FileExtension { get; set; } = default!;   // e.g. ".pdf"
    }

}
