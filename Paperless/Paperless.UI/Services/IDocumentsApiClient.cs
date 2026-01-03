namespace Paperless.UI.Services;

public interface IDocumentsApiClient
{
    Task<DocumentDto?> GetAsync(Guid id);
    Task<DocumentTextDto?> GetTextAsync(Guid id);
}

public class DocumentDto
{
    public Guid id { get; set; }
    public DocumentMetadataDto? metadata { get; set; }
    public DocumentFileDto? file { get; set; }
}

public class DocumentMetadataDto
{
    public string? title { get; set; }
    public string? description { get; set; }
    public string? languageCode { get; set; }
    public DateTimeOffset createdAt { get; set; }
}

public class DocumentFileDto
{
    public string? originalFileName { get; set; }
    public DateTimeOffset? uploadedAt { get; set; }
}

public class DocumentTextDto
{
    public Guid documentId { get; set; }
    public string? ocr { get; set; }
    public string? summary { get; set; }
    public DateTime timestamp { get; set; }
}
