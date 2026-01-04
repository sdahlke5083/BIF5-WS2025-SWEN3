using System.Net;
using System.Net.Http.Json;

namespace Paperless.UI.Services;

public class DocumentsApiClient : IDocumentsApiClient
{
    private readonly HttpClient _http;
    public DocumentsApiClient(HttpClient http) => _http = http;

    public Task<DocumentDto?> GetAsync(Guid id)
        => _http.GetFromJsonAsync<DocumentDto>($"/v1/documents/{id}");

    public async Task<DocumentTextDto?> GetTextAsync(Guid id)
    {
        var resp = await _http.GetAsync($"/v1/documents/{id}/text");
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<DocumentTextDto>();
    }

    public async Task<byte[]?> GetThumbnailPngAsync(Guid id)
    {
        var resp = await _http.GetAsync($"/v1/documents/{id}/thumbnail");
        if (resp.StatusCode == HttpStatusCode.NotFound) 
            return null;

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync();
    }

    public async Task<DocumentListPageDto> ListAsync(string? q = "", int page = 1, int pageSize = 50)
    {
        var url = $"/v1/documents?q={Uri.EscapeDataString(q ?? string.Empty)}&page={page}&pageSize={pageSize}";
        var resp = await _http.GetFromJsonAsync<DocumentListPageDto>(url);
        return resp ?? new DocumentListPageDto { page = page, pageSize = pageSize, total = 0, items = new() };
    }

}