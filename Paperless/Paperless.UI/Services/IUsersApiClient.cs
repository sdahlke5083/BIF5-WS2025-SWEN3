using System;
using System.Threading.Tasks;

namespace Paperless.UI.Services
{
    public interface IUsersApiClient
    {
        Task<UserProfileDto?> GetProfileAsync();
        Task UpdateProfileAsync(UserProfileUpdateDto dto);
    }

    public record UserProfileDto(Guid id, string username, string? displayName, bool mustChangePassword);
    public record UserProfileUpdateDto(string? displayName, string? password, string? currentPassword);
}
