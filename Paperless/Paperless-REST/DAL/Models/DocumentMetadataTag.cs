namespace Paperless.REST.DAL.Models
{
    public sealed class DocumentMetadataTag
    {
        public Guid DocumentId { get; set; }
        public int Version { get; set; }
        public Guid TagId { get; set; }

        public DocumentMetadata Metadata { get; set; } = default!;
        public Tag Tag { get; set; } = default!;
    }
}
