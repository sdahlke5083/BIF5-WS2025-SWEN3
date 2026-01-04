using Microsoft.AspNetCore.Components;
using Paperless.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Paperless.UI.Components.Pages;

public partial class Search
{
    [Inject]
    public Services.IUploadsApiClient Api { get; set; } = default!;

    [Inject]
    public IDocumentsApiClient DocsApi { get; set; } = default!;

    private string Query { get; set; } = string.Empty;
    private List<SearchDocumentItem>? Results { get; set; }
    private bool IsLoading { get; set; }

    private async Task DoSearch()
    {
        if (string.IsNullOrWhiteSpace(Query))
            return;

        IsLoading = true;
        Results = null;

        try
        {
            var apiResults = await Api.SearchDetailedAsync(Query);

            Results = apiResults
                .Select(r => new SearchDocumentItem
                {
                    Id = r.id,
                    FileName = r.fileName,
                    UploadedAt = r.uploadedAt,
                    OcrPreview = r.ocrPreview,
                    SummaryPreview = r.summaryPreview,
                    HasSummary = r.hasSummary
                })
                .ToList();

            var thumbTasks = Results.Select(LoadThumbnailAsync).ToArray();
            await Task.WhenAll(thumbTasks);
        }
        catch (Exception ex)
        {
            Results = new List<SearchDocumentItem>();
            Console.WriteLine(ex.Message);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadThumbnailAsync(SearchDocumentItem item)
    {
        try
        {
            var thumb = await DocsApi.GetThumbnailPngAsync(item.Id);
            if (thumb is not null && thumb.Length > 0)
            {
                item.ThumbnailDataUrl = "data:image/png;base64," + Convert.ToBase64String(thumb);
            }
        }
        catch
        {
            // ignore per item -> UI shows "(no preview)"
        }
    }

    private sealed class SearchDocumentItem
    {
        public Guid Id { get; set; }
        public string? FileName { get; set; }
        public DateTimeOffset? UploadedAt { get; set; }
        public string? OcrPreview { get; set; }
        public string? SummaryPreview { get; set; }
        public bool HasSummary { get; set; }
        public string? ThumbnailDataUrl { get; set; }
    }
}
