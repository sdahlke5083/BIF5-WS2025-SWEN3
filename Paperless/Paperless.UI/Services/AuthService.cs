using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Paperless.UI.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private string? _token;
        private bool _loaded = false;

        private const string TokenKey = "paperless.token";

        public AuthService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        public string? Token => _token;

        public async Task EnsureLoadedAsync()
        {
            if (_loaded) return;
            try
            {
                var t = await _js.InvokeAsync<string>("localStorage.getItem", TokenKey);
                if (!string.IsNullOrWhiteSpace(t)) _token = t;
            }
            catch { }
            finally { _loaded = true; }
        }

        public event Action? TokenChanged;

        public async Task<bool> LoginAsync(string username, string password)
        {
            var req = new { username, password };
            var resp = await _http.PostAsJsonAsync("/v1/auth/login", req);
            if (!resp.IsSuccessStatusCode) return false;
            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("token", out var t))
            {
                _token = t.GetString();
                if (!string.IsNullOrWhiteSpace(_token))
                {
                    try { await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, _token); } catch { }
                }
                TokenChanged?.Invoke();
                return !string.IsNullOrWhiteSpace(_token);
            }
            return false;
        }

        public void Logout()
        {
            _token = null;
            try { _ = _js.InvokeVoidAsync("localStorage.removeItem", TokenKey); } catch { }
            TokenChanged?.Invoke();
        }
    }
}
