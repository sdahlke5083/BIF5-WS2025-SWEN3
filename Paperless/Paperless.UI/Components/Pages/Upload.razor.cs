using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Paperless.UI.Services;
using System.Threading.Tasks;

namespace Paperless.UI.Components.Pages;

public partial class Upload
{
    [Inject]
    public IUploadsApiClient Api { get; set; } = default!;

    private IBrowserFile? SelectedFile;
    private string Status = string.Empty;

    private void OnChange(InputFileChangeEventArgs e)
    {
        SelectedFile = e.File;
    }

    private async Task UploadFile()
    {
        if (SelectedFile == null) return;
        Status = "Uploading...";
        try
        {
            var res = await Api.UploadAsync(SelectedFile);
            Status = $"Uploaded {res.saved?.Length ?? 0} files";
        }
        catch (System.Exception ex)
        {
            Status = "Upload failed: " + ex.Message;
        }
    }
}
