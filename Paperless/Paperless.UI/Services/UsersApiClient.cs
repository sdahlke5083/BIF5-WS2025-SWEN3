using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Components;

namespace Paperless.UI.Services
{
    public class UsersApiClient : ApiClientBase, IUsersApiClient
    {
        public UsersApiClient(HttpClient http, IAuthService auth, NavigationManager nav) : base(http, auth, nav) { }

        public async Task<UserProfileDto?> GetProfileAsync()
        {
            return await GetOrHandleUnauthorizedAsync<UserProfileDto>("/v1/users/me");
        }

        public async Task UpdateProfileAsync(UserProfileUpdateDto dto)
        {
            var resp = await PatchOrHandleUnauthorizedAsync("/v1/users/me", dto);
            resp.EnsureSuccessStatusCode();
        }
    }
}
