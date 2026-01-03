using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using System.Net.Http.Json;
using System.Collections.Generic;

namespace Paperless.UI.Services
{
    public class UploadsApiClient : IUploadsApiClient
    {
        private readonly HttpClient _http;

        public UploadsApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<UploadResult> UploadAsync(IBrowserFile file, CancellationToken ct = default)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                var metadataFiles = new Dictionary<string, object?>();

                    await using var stream = file.OpenReadStream(long.MaxValue, ct);
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                    content.Add(fileContent, "files", file.Name);

                    // Metadaten für jede Datei sammeln
                    metadataFiles[file.Name] = new
                    {
                        title = file.Name,
                        description = file.Name,
                        languagecode = "en"
                    };

                // Metadaten-Objekt im erwarteten Format
                var metadataObj = new Dictionary<string, object?>
                {
                    ["files"] = metadataFiles
                };

                var metadataJson = JsonSerializer.Serialize(metadataObj);
                var metadataContent = new StringContent(metadataJson, System.Text.Encoding.UTF8, "application/json");
                content.Add(metadataContent, "metadata");

                // Use leading slash to match the controller's route attribute [Route("/v1/uploads")]
                var response = await _http.PostAsync("/v1/uploads", content, CancellationToken.None);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadAsStringAsync(ct);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<UploadResult>(payload, options) ?? new UploadResult();
                return result;
            }
            catch (HttpRequestException ex)
            {
                // More detailed error information
                var details = ex.Data.Contains("Details") ? ex.Data["Details"] : "N/A";
                throw new Exception($"API connection error: {ex.Message} (StatusCode: {ex.StatusCode})\nEx: {details} ", ex);
            }
        }

        public async Task<List<Guid>> SearchAsync(string q, int page = 1, int pageSize = 20)
        {
            var from = (page - 1) * pageSize;
            var url = $"/v1/search?q={Uri.EscapeDataString(q)}&page={page}&pageSize={pageSize}";
            var resp = await _http.GetFromJsonAsync<SearchResponseDto>(url);
            return resp?.ids ?? new List<Guid>();
        }

        private class SearchResponseDto { public List<Guid> ids { get; set; } = new(); }

        public async Task<List<SearchResultItemDto>> SearchDetailedAsync(string q, int page = 1, int pageSize = 20)
        {
            var url = $"/v1/search?q={Uri.EscapeDataString(q)}&page={page}&pageSize={pageSize}";
            var resp = await _http.GetFromJsonAsync<SearchDetailedResponseDto>(url);
            return resp?.items ?? new List<SearchResultItemDto>();
        }

        private class SearchDetailedResponseDto
        {
            public List<SearchResultItemDto> items { get; set; } = new();
        }

    }
}
