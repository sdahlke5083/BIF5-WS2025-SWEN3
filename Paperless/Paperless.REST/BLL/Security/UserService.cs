using Microsoft.EntityFrameworkCore;
using Paperless.REST.API.Models;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.DAL.Models;

namespace Paperless.REST.BLL.Security
{
    /// <summary>
    /// Provides user account management operations, including authentication, profile retrieval and updates, and user listing.
    /// </summary>
    /// <remarks>The <see cref="UserService"/> class offers methods for validating user credentials,
    /// retrieving and updating user profiles, and listing users. It is intended to be used as the main entry point for
    /// user-related business logic in the application.</remarks>
    public class UserService : IUserService
    {
        private readonly PostgressDbContext _db;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class using the specified database context.
        /// </summary>
        /// <param name="db">The <see cref="PostgressDbContext"/> to be used for data access operations related to users. Cannot be null.</param>
        public UserService(PostgressDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Validates the specified username and password against stored user credentials.
        /// </summary>
        /// <remarks>Returns <see langword="null"/> if the username does not exist or if the password does
        /// not match the stored credentials.</remarks>
        /// <param name="username">The username to authenticate. Cannot be null, empty, or consist only of white-space characters.</param>
        /// <param name="password">The password to authenticate. Cannot be null, empty, or consist only of white-space characters.</param>
        /// <returns>The <see cref="User"/> object representing the authenticated user if the credentials are valid; otherwise,
        /// <see langword="null"/>.</returns>
        public User? ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user is null) return null;

            if (!PasswordHasher.Verify(password, user.Password))
                return null;

            return user;
        }

        /// <summary>
        /// Retrieves the user profile associated with the specified user identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user whose profile is to be retrieved.</param>
        /// <returns>A <see cref="UserProfileDto"/> containing the user's profile information if a user with the specified
        /// identifier exists; otherwise, <see langword="null"/>.</returns>
        public UserProfileDto? GetProfile(Guid id)
        {
            var user = _db.Users.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (user is null) return null;
            return new UserProfileDto { Id = user.Id, Username = user.Username, DisplayName = user.DisplayName, MustChangePassword = user.MustChangePassword };
        }

        /// <summary>
        /// Updates the profile information for the specified user.
        /// </summary>
        /// <remarks>If a password change is requested and <paramref name="requireCurrentPassword"/> is
        /// <see langword="true"/>, the current password must be provided and correct. The method validates the new
        /// password and updates the user's profile accordingly. If the user is not found, the update fails.</remarks>
        /// <param name="id">The unique identifier of the user whose profile is to be updated.</param>
        /// <param name="req">An object containing the updated profile information, such as display name and password.</param>
        /// <param name="requireCurrentPassword"><see langword="true"/> to require the current password when changing the user's password; otherwise, <see
        /// langword="false"/>.</param>
        /// <returns>A <see cref="UserUpdateResult"/> indicating whether the update was successful. If the update fails, the
        /// result includes error messages describing the reason.</returns>
        public UserUpdateResult UpdateProfile(Guid id, UserProfileUpdateRequest req, bool requireCurrentPassword = true)
        {
            var user = _db.Users.FirstOrDefault(x => x.Id == id);
            if (user is null) return new UserUpdateResult { Success = false };

            if (!string.IsNullOrWhiteSpace(req.DisplayName))
                user.DisplayName = req.DisplayName!;

            if (!string.IsNullOrWhiteSpace(req.Password))
            {
                if (requireCurrentPassword)
                {
                    if (string.IsNullOrWhiteSpace(req.CurrentPassword))
                        return new UserUpdateResult { Success = false, Errors = new List<string> { "Current password is required to change password." } };
                    if (!PasswordHasher.Verify(req.CurrentPassword!, user.Password))
                        return new UserUpdateResult { Success = false, Errors = new List<string> { "Current password is incorrect." } };
                }

                var errors = PasswordValidator.Validate(req.Password!);
                if (errors.Count > 0)
                    return new UserUpdateResult { Success = false, Errors = (List<string>)errors };

                user.Password = PasswordHasher.Hash(req.Password!);
                user.MustChangePassword = false;
            }

            _db.SaveChanges();
            return new UserUpdateResult { Success = true, Id = user.Id };
        }

        /// <summary>
        /// Updates the profile information of the specified user as an administrator, bypassing the requirement for the
        /// user's current password.
        /// </summary>
        /// <remarks>This method is intended for administrative use and does not require the user's
        /// current password to perform the update. Use this method only when appropriate permissions have been
        /// granted.</remarks>
        /// <param name="id">The unique identifier of the user whose profile is to be updated.</param>
        /// <param name="req">An object containing the updated profile information to apply to the user.</param>
        /// <returns>A <see cref="UserUpdateResult"/> that indicates the outcome of the update operation, including any
        /// validation errors or status information.</returns>
        public UserUpdateResult UpdateUserAsAdmin(System.Guid id, UserProfileUpdateRequest req)
        {
            return UpdateProfile(id, req, requireCurrentPassword: false);
        }

        /// <summary>
        /// Retrieves a collection of users with summary information for each user.
        /// </summary>
        /// <returns>?An <see cref="IEnumerable{T}"/> of <see cref="UserListDto"/> objects, each containing the ID, username, and
        /// display name of a user.  The collection is empty if no users are found.</returns>
        public IEnumerable<UserListDto> ListUsers()
        {
            return _db.Users.AsNoTracking().Select(u => new UserListDto { Id = u.Id, Username = u.Username, DisplayName = u.DisplayName }).ToList();
        }

        /// <summary>
        /// Asynchronously retrieves a user by identifier, including the user's associated roles.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="User"/> with
        /// populated role information if found; otherwise, <see langword="null"/>.</returns>
        public async Task<User?> GetUserWithRolesAsync(Guid userId)
        {
            return await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
