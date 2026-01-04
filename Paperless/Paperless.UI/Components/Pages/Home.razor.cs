using Microsoft.AspNetCore.Components;
using Paperless.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Paperless.UI.Components.Pages;

public partial class Home
{
    [Inject]
    public IDocumentsApiClient DocsApi { get; set; } = default!;

    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private List<HomeDocumentItem> Documents { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Documents = new List<HomeDocumentItem>();

        try
        {
            // Liste ALLER Dokumente aus der DB
            await Task.Delay(2000); // TODO: Hotfix Provisorisch, lösung gefunden da bei 
                                    // erstem aufruf die api fehlschlägt da alles starten muss
            var page = await DocsApi.ListAsync(q: "", page: 1, pageSize: 200);

            Documents = page.items
                .OrderByDescending(i => i.uploadedAt ?? DateTimeOffset.MinValue)
                .Select(i => new HomeDocumentItem
                {
                    Id = i.id,
                    FileName = !string.IsNullOrWhiteSpace(i.fileName) ? i.fileName : i.title,
                    UploadedAt = i.uploadedAt
                })
                .ToList();

            // OCR + Summary pro Dokument aus ES holen (/v1/documents/{id}/text)
            var loadTasks = Documents.Select(LoadTextAsync).ToArray();
            await Task.WhenAll(loadTasks);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTextAsync(HomeDocumentItem item)
    {
        try
        {
            var text = await DocsApi.GetTextAsync(item.Id);
            item.Ocr = text?.ocr;
            item.Summary = text?.summary;
            var thumb = await DocsApi.GetThumbnailPngAsync(item.Id);
            if (thumb is not null && thumb.Length > 0)
            {
                item.ThumbnailDataUrl = "data:image/png;base64," + Convert.ToBase64String(thumb);
            }
        }
        catch
        {
            // Pro Dokument ignorieren: UI zeigt "(not available yet)"
        }
    }

    private sealed class HomeDocumentItem
    {
        public Guid Id { get; set; }
        public string? FileName { get; set; }
        public DateTimeOffset? UploadedAt { get; set; }
        public string? Ocr { get; set; }
        public string? Summary { get; set; }
        public string? ThumbnailDataUrl { get; set; }
    }
}
