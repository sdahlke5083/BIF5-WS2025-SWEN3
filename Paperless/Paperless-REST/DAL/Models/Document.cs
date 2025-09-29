using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    [Index(nameof(DeletedAt))]
    public sealed class Document
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }

        // Versioning
        public int CurrentMetadataVersion { get; set; } = 1;
        public int CurrentFileVersion { get; set; } = 1;

        // Soft delete
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedByUserId { get; set; }

        // workspaces
        public Guid? WorkspaceId { get; set; }

        // EF Core references
        public User? DeletedByUser { get; set; }
        public Workspace? Workspace { get; set; }
        public ProcessingStatus ProcessingStatus { get; set; } = new();
        public ICollection<DocumentMetadata> MetadataVersions { get; set; } = new List<DocumentMetadata>();
        public ICollection<DocumentTag> DocumentTags { get; set; } = new List<DocumentTag>();
        public ICollection<FileVersion> FileVersions { get; set; } = new List<FileVersion>();
        public ICollection<Summary> Summaries { get; set; } = new List<Summary>();
    }
}
