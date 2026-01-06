using Paperless.REST.API.Models;
using Paperless.REST.DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Paperless.REST.BLL.Security
{
    public sealed class UserListDto { public Guid Id { get; set; } public string Username { get; set; } = default!; public string? DisplayName { get; set; } }
    public sealed class UserProfileDto { public Guid Id { get; set; } public string Username { get; set; } = default!; public string? DisplayName { get; set; } public bool MustChangePassword { get; set; } }
    public sealed class UserUpdateResult { public bool Success { get; set; } public List<string> Errors { get; set; } = new(); public Guid? Id { get; set; } }

    /// <summary>
    /// Defines operations for authenticating users, retrieving and updating user profiles, and managing user accounts
    /// within the system.
    /// </summary>
    /// <remarks>The <see cref="IUserService"/> interface provides methods for validating user credentials,
    /// accessing and modifying user profile information, and performing administrative user management tasks. It
    /// supports both synchronous and asynchronous operations, and distinguishes between actions performed by the
    /// current user and those performed by an administrator.</remarks>
    public interface IUserService
    {
        /// <summary>
        /// Validates the specified username and password against the user store.
        /// </summary>
        /// <param name="username">The username to authenticate. Cannot be null or empty.</param>
        /// <param name="password">The password associated with the specified username. Cannot be null or empty.</param>
        /// <returns>A <see cref="User"/> object representing the authenticated user if the credentials are valid; otherwise,
        /// <see langword="null"/>.</returns>
        User? ValidateCredentials(string username, string password);

        /// <summary>
        /// Retrieves the user profile associated with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user whose profile is to be retrieved.</param>
        /// <returns>A <see cref="UserProfileDto"/> representing the user's profile if found; otherwise, <see langword="null"/>.</returns>
        UserProfileDto? GetProfile(Guid id);

        /// <summary>
        /// Updates the profile information for the specified user.
        /// </summary>
        /// <remarks>The update may fail if the provided profile information does not meet validation
        /// requirements or if the current password is required and not provided or incorrect.</remarks>
        /// <param name="id">The unique identifier of the user whose profile is to be updated.</param>
        /// <param name="req">An object containing the updated profile information. Cannot be <see langword="null"/>.</param>
        /// <param name="requireCurrentPassword"><see langword="true"/> to require the user's current password for the update; otherwise, <see
        /// langword="false"/>.</param>
        /// <returns>A <see cref="UserUpdateResult"/> indicating the outcome of the update operation, including success status
        /// and any validation errors.</returns>
        UserUpdateResult UpdateProfile(Guid id, UserProfileUpdateRequest req, bool requireCurrentPassword = true);

        /// <summary>
        /// Updates the specified user's profile with administrative privileges.
        /// </summary>
        /// <param name="id">The unique identifier of the user to update.</param>
        /// <param name="req">An object containing the updated profile information to apply to the user. Cannot be <see langword="null"/>.</param>
        /// <returns>A <see cref="UserUpdateResult"/> indicating the outcome of the update operation, including any validation
        /// errors or status information.</returns>
        UserUpdateResult UpdateUserAsAdmin(Guid id, UserProfileUpdateRequest req);

        /// <summary>
        /// Retrieves a collection of users currently registered in the system.
        /// </summary>
        /// <returns>?An <see cref="IEnumerable{T}"/> of <see cref="UserListDto"/> objects representing the users.  The
        /// collection is empty if no users are found.</returns>
        IEnumerable<UserListDto> ListUsers();

        /// <summary>
        /// Asynchronously retrieves a user and their associated roles by user identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="User"/> object
        /// with populated role information if the user exists; otherwise, <see langword="null"/>.</returns>
        Task<User?> GetUserWithRolesAsync(System.Guid userId);
    }
}
