using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Components;

namespace Paperless.UI.Services
{
    public class WorkspacesApiClient : ApiClientBase, IWorkspacesApiClient
    {
        public WorkspacesApiClient(HttpClient http, IAuthService auth, NavigationManager nav) : base(http, auth, nav) { }

        public async Task<List<WorkspaceDto>> ListAsync()
        {
            return await GetOrHandleUnauthorizedAsync<List<WorkspaceDto>>("/v1/workspaces") ?? new List<WorkspaceDto>();
        }

        public async Task<Guid> CreateAsync(string name, string? description)
        {
            var obj = new { name, description };
            var resp = await PostOrHandleUnauthorizedAsync("/v1/workspaces", obj);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
            return body != null && body.TryGetValue("id", out var id) ? id : Guid.Empty;
        }

        public async Task DeleteAsync(Guid id)
        {
            var resp = await DeleteOrHandleUnauthorizedAsync($"/v1/workspaces/{id}");
            resp.EnsureSuccessStatusCode();
        }
    }
}
