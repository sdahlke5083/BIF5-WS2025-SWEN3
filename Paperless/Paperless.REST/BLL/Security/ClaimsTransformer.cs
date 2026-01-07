using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Paperless.REST.DAL.DbContexts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Paperless.REST.BLL.Security
{
    /// <summary>
    /// Transforms an authenticated principal by fetching roles from the database and adding them as role claims.
    /// </summary>
    public class ClaimsTransformer : IClaimsTransformation
    {
        private readonly IUserService _userService;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Transforms an authenticated principal by fetching roles from the database and adding them as role claims.
        /// </summary>
        public ClaimsTransformer(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Enriches the specified <see cref="ClaimsPrincipal"/> with role claims based on the user's roles from the
        /// database.
        /// </summary>
        /// <remarks>This method adds role claims to the principal if they are not already present, based
        /// on the roles assigned to the user in the database. The user identifier is obtained from the <c>sub</c> (JWT
        /// subject) or <see cref="ClaimTypes.NameIdentifier"/> claim. If an error occurs during database access, the
        /// exception is logged and the original principal is returned.</remarks>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> to transform. Must represent an authenticated user and contain a valid
        /// user identifier claim.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> that includes role claims corresponding to the user's roles in the database.
        /// If the user is not authenticated, lacks a valid user identifier, or is not found in the database, the
        /// original principal is returned unchanged.</returns>
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal?.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
                return principal;

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                      principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(sub, out var userId))
            {
                return principal;
            }

            try
            {
                var user = await _userService.GetUserWithRolesAsync(userId);

                if (user == null || user.UserRoles.Count <= 0)
                {
                    return principal;
                }

                foreach (var userRole in user.UserRoles)
                {
                    var roleName = userRole.Role?.Name;
                    if (string.IsNullOrWhiteSpace(roleName)) continue;

                    // only add if not present yet
                    if (!principal.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleName))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while transforming claims for user {UserId}", userId);
            }

            return principal;
        }
    }
}
