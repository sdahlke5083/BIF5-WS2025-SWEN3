using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Paperless.UI.Services;

namespace Paperless.UI.Components.Pages;

public class LoginBase : ComponentBase
{
    [Inject]
    public IAuthService Auth { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    protected string Username { get; set; } = string.Empty;
    protected string Password { get; set; } = string.Empty;
    protected string? Error { get; set; }

    protected async Task DoLogin()
    {
        Error = null;
        var ok = await Auth.LoginAsync(Username, Password);
        if (ok)
        {
            Navigation.NavigateTo("/");
        }
        else
        {
            Error = "Login failed";
        }
    }
}
