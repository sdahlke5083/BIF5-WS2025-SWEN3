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
}