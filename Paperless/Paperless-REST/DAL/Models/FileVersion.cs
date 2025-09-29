namespace Paperless.REST.DAL.Models
{
    public sealed class FileVersion
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = default!;
        public int Version { get; set; }
        public string StoredName { get; set; } = default!;
        public string ContentSha256 { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
    }

}
