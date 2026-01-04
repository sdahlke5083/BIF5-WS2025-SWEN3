namespace Paperless.UI.Services;

public interface IDocumentsApiClient
{
    Task<DocumentDto?> GetAsync(Guid id);
    Task<DocumentTextDto?> GetTextAsync(Guid id);
    Task<DocumentListPageDto> ListAsync(string? q = "", int page = 1, int pageSize = 50);
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

public class DocumentListPageDto
{
    public int page { get; set; }
    public int pageSize { get; set; }
    public int total { get; set; }
    public List<DocumentListItemDto> items { get; set; } = new();
}

public class DocumentListItemDto
{
    public Guid id { get; set; }
    public string? title { get; set; }
    public string? fileName { get; set; }
    public DateTimeOffset? uploadedAt { get; set; }
    public long? size { get; set; }
}