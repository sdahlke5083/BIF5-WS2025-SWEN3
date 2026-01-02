using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Paperless.UI.Services
{
    public interface IWorkspacesApiClient
    {
        Task<List<WorkspaceDto>> ListAsync();
        Task<Guid> CreateAsync(string name, string? description);
        Task DeleteAsync(Guid id);
    }

    public record WorkspaceDto(Guid id, string name, string? description);
}
