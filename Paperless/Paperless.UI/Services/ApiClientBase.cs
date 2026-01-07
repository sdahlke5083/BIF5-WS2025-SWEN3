using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;

namespace Paperless.UI.Services
{
    public abstract class ApiClientBase
    {
        protected readonly HttpClient _http;
        protected readonly IAuthService _auth;
        protected readonly NavigationManager _nav;

        protected ApiClientBase(HttpClient http, IAuthService auth, NavigationManager nav)
        {
            _http = http;
            _auth = auth;
            _nav = nav;
            // ensure HttpClient default auth header reflects current token and updates when it changes
            try
            {
                UpdateAuthHeader();
                _auth.TokenChanged += UpdateAuthHeader;
            }
            catch { }
        }

        private void UpdateAuthHeader()
        {
            try
            {
                var token = _auth.Token;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                else
                {
                    _http.DefaultRequestHeaders.Authorization = null;
                }
            }
            catch { }
        }

        protected async Task<T?> GetOrHandleUnauthorizedAsync<T>(string url)
        {
            var resp = await _http.GetAsync(url);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleUnauthorized();
                return default;
            }
            return await resp.Content.ReadFromJsonAsync<T>();
        }

        protected async Task<HttpResponseMessage> PostOrHandleUnauthorizedAsync(string url, object? body)
        {
            var resp = await _http.PostAsJsonAsync(url, body);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleUnauthorized();
            }
            return resp;
        }

        protected async Task<HttpResponseMessage> PatchOrHandleUnauthorizedAsync(string url, object? body)
        {
            // HttpClient doesn't have PatchAsJsonAsync until .NET 7; use Send
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = JsonContent.Create(body)
            };
            var resp = await _http.SendAsync(req);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleUnauthorized();
            }
            return resp;
        }

        protected async Task<HttpResponseMessage> DeleteOrHandleUnauthorizedAsync(string url)
        {
            var resp = await _http.DeleteAsync(url);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleUnauthorized();
            }
            return resp;
        }

        private void HandleUnauthorized()
        {
            try { _auth.Logout(); } catch { }
            try { _nav.NavigateTo("/login"); } catch { }
        }
    }
}
