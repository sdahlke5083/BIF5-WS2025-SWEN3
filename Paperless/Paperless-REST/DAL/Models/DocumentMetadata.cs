namespace Paperless.REST.DAL.Models
{
    public sealed class DocumentMetadata
    {
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = default!;
        public int Version { get; set; }                              // 1..N
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }

        // Language lookup FK (nullable) + enum in code
        public int? LanguageId { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DocumentLanguage? Language
        {
            get => LanguageId.HasValue ? (DocumentLanguage)LanguageId.Value : null;
            set => LanguageId = value.HasValue ? (int)value.Value : null;
        }

        public ICollection<DocumentMetadataTag> Tags { get; set; } = new List<DocumentMetadataTag>();
    }
}
