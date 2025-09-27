namespace DAL.Models
{
    public sealed class Document
    {
        public Guid Id { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
        public Guid? UploadedByUserId { get; set; }
        public User? UploadedByUser { get; set; }
        public string OriginalFileName { get; set; } = default!;
        public long SizeBytes { get; set; }

        // Content type lookup FK (int in DB) + enum in code
        public int ContentTypeId { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DocumentContentType ContentType
        {
            get => (DocumentContentType)ContentTypeId;
            set => ContentTypeId = (int)value;
        }

        // Fast pointer to current metadata version
        public int CurrentMetadataVersion { get; set; } = 1;

        // Soft delete
        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedByUserId { get; set; }
        public User? DeletedByUser { get; set; }

        public Guid? WorkspaceId { get; set; }
        public Workspace? Workspace { get; set; }

        // Navs
        public ICollection<DocumentMetadata> MetadataVersions { get; set; } = new List<DocumentMetadata>();
        public ProcessingStatus ProcessingStatus { get; set; } = new();
        public ICollection<FileVersion> FileVersions { get; set; } = new List<FileVersion>();
        public ICollection<Summary> Summaries { get; set; } = new List<Summary>();
    }
}
