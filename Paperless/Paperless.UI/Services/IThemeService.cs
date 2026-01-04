using System.Threading.Tasks;

namespace Paperless.UI.Services
{
    public interface IThemeService
    {
        Task SetThemeAsync(string theme);
        Task<string> GetThemeAsync();
        Task ToggleThemeAsync();
        event System.Action<string>? ThemeChanged;
    }
}
