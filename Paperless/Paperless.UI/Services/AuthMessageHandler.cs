using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Paperless.UI.Services
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly IAuthService _auth;
        private readonly NavigationManager _nav;
        private readonly INotificationService _notify;

        public AuthMessageHandler(IAuthService auth, NavigationManager nav, INotificationService notify)
        {
            _auth = auth;
            _nav = nav;
            _notify = notify;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // ensure auth token loaded (in case AuthService uses JS to persist token)
            try { await _auth.EnsureLoadedAsync(); } catch { }

            var token = _auth.Token;

            // proactively check expiry
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    var exp = GetExpiryFromJwt(token);
                    if (exp.HasValue && exp.Value <= DateTimeOffset.UtcNow)
                    {
                        // token expired -> logout and redirect with notification
                        try { _auth.Logout(); } catch { }
                        try { _notify.Notify("Session expired. Please login again.", "warning"); } catch { }
                        try { _nav.NavigateTo("/login"); } catch { }
                        return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized) { RequestMessage = request };
                    }
                }
                catch { /* ignore parse errors */ }

                if (!request.Headers.Contains("Authorization"))
                    request.Headers.Add("Authorization", "Bearer " + token);
            }

            var resp = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // if server says unauthorized, surface notification
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                try { _notify.Notify("You have been logged out. Please sign in again.", "warning"); } catch { }
            }

            return resp;
        }

        private static DateTimeOffset? GetExpiryFromJwt(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return null;
                string payload = parts[1];
                // base64url -> base64
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                var bytes = System.Convert.FromBase64String(payload);
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("exp", out var expEl) && expEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var seconds = expEl.GetInt64();
                    return DateTimeOffset.FromUnixTimeSeconds(seconds);
                }
            }
            catch { }
            return null;
        }
    }
}
