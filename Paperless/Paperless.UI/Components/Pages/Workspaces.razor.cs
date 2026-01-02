using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Paperless.UI.Components.Pages;

public class WorkspacesBase : ComponentBase
{
    protected List<dynamic>? Workspaces;
    protected string NewName = string.Empty;

    protected override Task OnInitializedAsync()
    {
        // Placeholder - would call API
        Workspaces = new List<dynamic> { new { id = System.Guid.NewGuid(), name = "Default" } };
        return Task.CompletedTask;
    }

    protected Task CreateWorkspace()
    {
        // Placeholder
        Workspaces!.Add(new { id = System.Guid.NewGuid(), name = NewName });
        NewName = string.Empty;
        return Task.CompletedTask;
    }

    protected Task DeleteWorkspace(System.Guid id)
    {
        Workspaces!.RemoveAll(w => (System.Guid)w.id == id);
        return Task.CompletedTask;
    }
}
