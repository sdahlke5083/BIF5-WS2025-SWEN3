using System.Threading.Tasks;

namespace Paperless.UI.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        string? Token { get; }
        void Logout();
        Task EnsureLoadedAsync();
        event Action? TokenChanged;
    }
}
