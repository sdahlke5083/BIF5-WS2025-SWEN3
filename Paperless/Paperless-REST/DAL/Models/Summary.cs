namespace Paperless.REST.DAL.Models
{
    public sealed class Summary
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public string? Model { get; set; }
        public string? LengthPreset { get; set; }
        public string Content { get; set; } = default!;
    }

}
