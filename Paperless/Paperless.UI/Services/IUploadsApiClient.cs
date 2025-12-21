using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace Paperless.UI.Services
{
    public interface IUploadsApiClient
    {
        Task<UploadResult> UploadAsync(IBrowserFile file, CancellationToken ct = default);
        Task<List<Guid>> SearchAsync(string q, int page = 1, int pageSize = 20);
    }

    public class UploadResult
    {
        public int accepted { get; set; }
        public string[] saved { get; set; } = new string[0];
        public string[] guids { get; set; } = new string[0];
    }
}
