using Microsoft.AspNetCore.Components;

namespace Paperless.UI.Components.Pages;

public partial class Search
{
    [Inject]
    public Services.IUploadsApiClient Api { get; set; } = default!;

    private string Query { get; set; } = string.Empty;
    private List<Guid>? Results { get; set; }
    private bool IsLoading { get; set; }

    private async Task DoSearch()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;
        IsLoading = true;
        Results = null;

        try
        {
            Results = await Api.SearchAsync(Query);
        }
        catch (Exception ex)
        {
            Results = new List<Guid>();
            Console.WriteLine(ex.Message);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private class SearchResponseDto { public List<Guid> ids { get; set; } = new(); }
}
