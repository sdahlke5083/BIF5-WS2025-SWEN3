using Microsoft.AspNetCore.Components;
using Paperless.UI.Services;

namespace Paperless.UI.Components.Pages;

public partial class Search
{
    [Inject]
    public Services.IUploadsApiClient Api { get; set; } = default!;

    private string Query { get; set; } = string.Empty;
    private List<SearchResultItemDto>? Results { get; set; }
    private bool IsLoading { get; set; }

    private async Task DoSearch()
    {
        if (string.IsNullOrWhiteSpace(Query)) 
            return;
        IsLoading = true;
        Results = null;

        try
        {
            Results = await Api.SearchDetailedAsync(Query);
        }
        catch (Exception ex)
        {
            Results = new List<SearchResultItemDto>();
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
