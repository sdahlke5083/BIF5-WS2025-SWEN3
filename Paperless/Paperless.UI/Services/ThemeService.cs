using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Paperless.UI.Services
{
    public class ThemeService : IThemeService
    {
        private readonly IJSRuntime _js;
        private const string Key = "paperless.theme";
        private string _current = "dark";
        public event System.Action<string>? ThemeChanged;

        public ThemeService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SetThemeAsync(string theme)
        {
            _current = theme;
            try { await _js.InvokeVoidAsync("localStorage.setItem", Key, theme); } catch { }
            ThemeChanged?.Invoke(theme);
        }

        public async Task<string> GetThemeAsync()
        {
            try
            {
                var v = await _js.InvokeAsync<string>("localStorage.getItem", Key);
                if (!string.IsNullOrWhiteSpace(v)) _current = v;
            }
            catch { }
            return _current;
        }

        public async Task ToggleThemeAsync()
        {
            var next = _current == "dark" ? "light" : "dark";
            await SetThemeAsync(next);
        }
    }
}
