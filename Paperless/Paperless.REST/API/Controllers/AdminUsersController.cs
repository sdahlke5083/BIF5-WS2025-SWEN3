using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.API.Models;
using Microsoft.EntityFrameworkCore;
using Paperless.REST.BLL.Security;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Provides administrative endpoints for managing user accounts. Accessible only to users with the "Admin" role.
    /// </summary>
    /// <remarks>This controller exposes operations for listing all users and updating user profiles. All
    /// actions require authentication and are restricted to users assigned the "Admin" role. The endpoints are intended
    /// for administrative use and are not accessible to regular users.</remarks>
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly PostgressDbContext _db;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminUsersController"/> class using the specified database
        /// context.
        /// </summary>
        /// <remarks>This constructor enables dependency injection of the database context, allowing the
        /// controller to interact with the underlying data store.</remarks>
        /// <param name="db">The <see cref="PostgressDbContext"/> to be used for data access operations.</param>
        public AdminUsersController(PostgressDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Retrieves a list of all users with their identifiers, usernames, and display names.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a collection of user objects. Each object includes the user's ID,
        /// username, and display name. The collection is empty if no users are found.</returns>
        [HttpGet]
        [Route("/v1/admin/users")]
        public IActionResult ListUsers()
        {
            var users = _db.Users.AsNoTracking().Select(u => new { id = u.Id, username = u.Username, displayName = u.DisplayName }).ToList();
            _logger.Debug("Admin user {admin} listed all users. Total users: {count}", User.Identity?.Name, users.Count);
            return Ok(users);
        }

        /// <summary>
        /// Updates the specified user's profile information.
        /// </summary>
        /// <remarks>Only the fields provided in <paramref name="req"/> that are not null or whitespace
        /// will be updated. If a new password is supplied, it must meet the application's password requirements;
        /// otherwise, the request will fail with a bad request response containing validation errors.</remarks>
        /// <param name="id">The unique identifier of the user to update.</param>
        /// <param name="req">An object containing the updated profile information for the user. Only non-empty fields will be applied.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns <see cref="OkResult"/> with
        /// the user ID if the update is successful; <see cref="NotFoundResult"/> if the user does not exist; or <see
        /// cref="BadRequestObjectResult"/> if the provided data is invalid.</returns>
        [HttpPatch]
        [Route("/v1/admin/users/{id}")]
        public IActionResult UpdateUser([FromRoute] Guid id, [FromBody] UserProfileUpdateRequest req)
        {
            var u = _db.Users.FirstOrDefault(x => x.Id == id);
            if (u is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.DisplayName)) u.DisplayName = req.DisplayName!;
            if (!string.IsNullOrWhiteSpace(req.Password))
            {
                var errors = PasswordValidator.Validate(req.Password!);
                if (errors.Count > 0)
                    return BadRequest(new { errors });

                u.Password = PasswordHasher.Hash(req.Password!);
                u.MustChangePassword = false;
            }

            _logger.Debug("Admin user {admin} updated user {user}", User.Identity?.Name, id);
            _db.SaveChanges();
            return Ok(new { id = u.Id });
        }
    }
}
