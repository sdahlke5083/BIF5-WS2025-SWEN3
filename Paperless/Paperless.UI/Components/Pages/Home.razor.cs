using Microsoft.AspNetCore.Components;
using Paperless.UI.Services;

namespace Paperless.UI.Components.Pages;

public partial class Home
{
    [Inject]
    public IDocumentsApiClient DocsApi { get; set; } = default!;

    [Inject]
    public IAuthService Auth { get; set; } = default!;

    [Inject]
    public NavigationManager Nav { get; set; } = default!;

    // Damit /ready pollen kann (plain client ohne Auth-Handler)
    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; } = default!;

    private bool IsLoading { get; set; } = true;
    private string LoadingMessage { get; set; } = "Loading...";
    private string? ErrorMessage { get; set; }
    private List<HomeDocumentItem> Documents { get; set; } = new();

    private bool _loadStarted;

    protected override Task OnInitializedAsync()
    {
        IsLoading = true;
        LoadingMessage = "Loading...";
        ErrorMessage = null;
        Documents = new List<HomeDocumentItem>();
        return Task.CompletedTask;
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        // Nach dem ersten Render startet das Laden im Hintergrund.
        if (firstRender && !_loadStarted)
        {
            _loadStarted = true;
            _ = LoadHomeAsync();
        }
        return Task.CompletedTask;
    }

    private async Task LoadHomeAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Documents = new List<HomeDocumentItem>();

        // Erst warten bis REST-API erreichbar/ready ist (Container starten)
        var ready = await WaitForBackendReadyAsync(TimeSpan.FromSeconds(60));
        if (!ready)
        {
            ErrorMessage = "Backend is not ready yet. Please start the containers (docker compose) and try again.";
            IsLoading = false;
            await SafeStateHasChangedAsync();
            return;
        }

        try
        {
            LoadingMessage = "Loading documents...";
            await SafeStateHasChangedAsync();

            // ensure auth token loaded before calling protected endpoints
            try { await Auth.EnsureLoadedAsync(); } catch { }
            if (string.IsNullOrWhiteSpace(Auth.Token))
            {
                // not authenticated -> redirect to login
                IsLoading = false;
                ErrorMessage = "Not authenticated";
                await SafeStateHasChangedAsync();
                try { Nav.NavigateTo("/login"); } catch { }
                return;
            }

            // Liste ALLER Dokumente aus der DB
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

            // OCR + Summary + Thumbnail pro Dokument laden
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
            LoadingMessage = "";
            await SafeStateHasChangedAsync();
        }
    }

    private async Task<bool> WaitForBackendReadyAsync(TimeSpan timeout)
    {
        // "auth" ist in Program.cs registriert (plain client, keine AuthMessageHandler-Logik)
        var http = HttpClientFactory.CreateClient("auth");

        var started = DateTimeOffset.UtcNow;
        var delayMs = 500;
        var attempt = 0;

        while (DateTimeOffset.UtcNow - started < timeout)
        {
            attempt++;
            LoadingMessage = $"Checking services... (attempt {attempt})";
            await SafeStateHasChangedAsync();

            try
            {
                // kurzer Timeout pro Versuch, damit es nicht ewig hängt
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                // /ready kommt aus AdminController in Paperless.REST
                var resp = await http.GetAsync("/ready", cts.Token);
                if (resp.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
                // connection refused / DNS / noch nicht gestartet -> einfach weiter warten
            }

            await Task.Delay(delayMs);
            delayMs = Math.Min((int)(delayMs * 1.5), 5000);
        }

        return false;
    }

    private async Task SafeStateHasChangedAsync()
    {
        try { await InvokeAsync(StateHasChanged); } catch { }
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
                item.ThumbnailDataUrl = "data:image/png;base64," + Convert.ToBase64String(thumb);
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
