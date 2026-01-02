using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Paperless.REST.API.Models;
using Paperless.REST.DAL.DbContexts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Provides authentication and user profile management endpoints for the application API.
    /// </summary>
    /// <remarks>The <see cref="AuthController"/> exposes endpoints for user login, retrieving the current
    /// user's profile, and updating profile information such as display name and password. All endpoints require
    /// appropriate request payloads and authentication where applicable. This controller is intended to be used as part
    /// of the application's REST API and relies on dependency-injected database and configuration services.</remarks>
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly PostgressDbContext _db;
        private readonly IConfiguration _config;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class with the specified database context and
        /// configuration settings.
        /// </summary>
        /// <remarks>Use this constructor to provide the necessary dependencies for authentication
        /// functionality. Both parameters must be non-null.</remarks>
        /// <param name="db">The database context used to access and manage authentication-related data.</param>
        /// <param name="config">The application configuration settings used for authentication operations.</param>
        public AuthController(PostgressDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        /// <summary>
        /// Authenticates a user using the provided credentials and issues a JSON Web Token (JWT) upon successful login.
        /// </summary>
        /// <remarks>This endpoint expects a login request containing a username and password. If
        /// authentication succeeds, a JWT is returned in the response body. The token can be used to authorize
        /// subsequent requests. <para> If authentication fails due to invalid credentials or a missing user, the
        /// response will indicate an error without revealing which part of the credentials was incorrect.
        /// </para></remarks>
        /// <param name="req">The login request containing the user's username and password. Must not be <see langword="null"/>.</param>
        /// <returns>An <see cref="IActionResult"/> containing a JWT token if authentication is successful; otherwise, a 400 Bad
        /// Request or 401 Unauthorized response.</returns>
        [HttpPost]
        [Route("/v1/auth/login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if (req is null) return BadRequest();

            var user = _db.Users.FirstOrDefault(u => u.Username == req.Username);
            if (user is null) return Unauthorized();

            // verify password: supports salted PBKDF2 stored as base64(salt|hash)
            if (!VerifyPassword(req.Password, user.Password))
                return Unauthorized();

            var key = _config["Jwt:Key"] ?? _config["Jwt__Key"] ?? "please_change_this_secret_in_production";
            var issuer = _config["Jwt:Issuer"] ?? _config["Jwt__Issuer"] ?? "paperless.local";
            var audience = _config["Jwt:Audience"] ?? _config["Jwt__Audience"] ?? "paperless.local";

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { token = tokenString });
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        [HttpGet]
        [Route("/v1/users/me")]
        public IActionResult GetProfile()
        {
            var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(sub, out var userId)) return Unauthorized();
            var u = _db.Users.AsNoTracking().FirstOrDefault(x => x.Id == userId);
            if (u is null) return NotFound();
            return Ok(new { id = u.Id, username = u.Username, displayName = u.DisplayName, mustChangePassword = u.MustChangePassword });
        }

        /// <summary>
        /// Update current user's profile (display name and/or password)
        /// </summary>
        [HttpPatch]
        [Route("/v1/users/me")]
        public IActionResult UpdateProfile([FromBody] UserProfileUpdateRequest req)
        {
            var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(sub, out var userId)) return Unauthorized();
            var u = _db.Users.FirstOrDefault(x => x.Id == userId);
            if (u is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.DisplayName)) u.DisplayName = req.DisplayName!;

            if (!string.IsNullOrWhiteSpace(req.Password))
            {
                // require current password for security
                if (string.IsNullOrWhiteSpace(req.CurrentPassword)) return BadRequest("Current password is required to change password.");
                if (!Paperless.REST.BLL.Security.PasswordHasher.Verify(req.CurrentPassword!, u.Password))
                    return BadRequest("Current password is incorrect.");

                // apply complexity rules
                var errors = Paperless.REST.BLL.Security.PasswordValidator.Validate(req.Password!);
                if (errors.Count > 0)
                    return BadRequest(new { errors });

                u.Password = Paperless.REST.BLL.Security.PasswordHasher.Hash(req.Password!);
                u.MustChangePassword = false;
            }

            _db.SaveChanges();
            return Ok(new { id = u.Id });
        }

        private static bool VerifyPassword(string providedPassword, string storedCombinedBase64)
        {
            if (string.IsNullOrEmpty(storedCombinedBase64)) return false;
            try
            {
                var combined = Convert.FromBase64String(storedCombinedBase64);
                if (combined.Length < 48) // 16 salt + 32 hash minimum
                    return false;

                var salt = new byte[16];
                Buffer.BlockCopy(combined, 0, salt, 0, 16);
                var hash = new byte[combined.Length - 16];
                Buffer.BlockCopy(combined, 16, hash, 0, hash.Length);

                using var derive = new System.Security.Cryptography.Rfc2898DeriveBytes(providedPassword, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
                var computed = derive.GetBytes(hash.Length);
                return ConstantTimeEquals(computed, hash);
            }
            catch
            {
                return false;
            }
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
