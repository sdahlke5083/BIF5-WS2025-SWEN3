using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System;

namespace Paperless.UI.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly ProtectedLocalStorage _storage;
        private string? _token;
        private bool _loaded = false;

        private const string TokenKey = "paperless.token";

        public AuthService(HttpClient http, ProtectedLocalStorage storage)
        {
            _http = http;
            _storage = storage;
        }

        public string? Token => _token;

        public async Task EnsureLoadedAsync()
        {
            if (_loaded) return;
            try
            {
                var prev = _token;
                var res = await _storage.GetAsync<string>(TokenKey);
                if (res.Success && !string.IsNullOrWhiteSpace(res.Value)) _token = res.Value;
                _loaded = true;

                // if a token was loaded now but previously was empty, notify subscribers
                if (string.IsNullOrWhiteSpace(prev) && !string.IsNullOrWhiteSpace(_token))
                {
                    try { TokenChanged?.Invoke(); } catch { }
                }
            }
            catch(Exception ex)
            {
                // If ProtectedLocalStorage isn't available for any reason, mark loaded so we don't loop.
                _loaded = true;
            }
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
                    try { await _storage.SetAsync(TokenKey, _token); } catch { }
                }
                TokenChanged?.Invoke();
                return !string.IsNullOrWhiteSpace(_token);
            }
            return false;
        }

        public void Logout()
        {
            _token = null;
            try { _ = _storage.DeleteAsync(TokenKey); } catch { }
            TokenChanged?.Invoke();
        }
    }
}
