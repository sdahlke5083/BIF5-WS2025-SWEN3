using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Paperless.REST.API.Models;
using Paperless.REST.BLL.Security;
using Paperless.REST.DAL.DbContexts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Paperless.REST.API.Controllers
{
    /// <summary>
    /// Provides authentication and jwt_username profile management endpoints for the application API.
    /// </summary>
    /// <remarks>The <see cref="AuthController"/> exposes endpoints for jwt_username login, retrieving the current
    /// jwt_username's profile, and updating profile information such as display name and password. All endpoints require
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
        /// Authenticates a jwt_username using the provided credentials and issues a JSON Web Token (JWT) upon successful login.
        /// </summary>
        /// <remarks>This endpoint expects a login request containing a username and password. If
        /// authentication succeeds, a JWT is returned in the response body. The token can be used to authorize
        /// subsequent requests. <para> If authentication fails due to invalid credentials or a missing jwt_username, the
        /// response will indicate an error without revealing which part of the credentials was incorrect.
        /// </para></remarks>
        /// <param name="req">The login request containing the jwt_username's username and password. Must not be <see langword="null"/>.</param>
        /// <returns>An <see cref="IActionResult"/> containing a JWT token if authentication is successful; otherwise, a 400 Bad
        /// Request or 401 Unauthorized response.</returns>
        [HttpPost]
        [Route("/v1/auth/login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if (req is null) return BadRequest();

            var user = _db.Users.FirstOrDefault(u => u.Username == req.Username);
            if (user is null) return Unauthorized();

            // verify password
            if (!PasswordHasher.Verify(req.Password, user.Password))
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
            _logger.Info("User '{0}' logged in successfully.", user.Username);

            return Ok(new { token = tokenString });
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        [HttpGet]
        [Route("/v1/users/me")]
        public IActionResult GetProfile()
        {
            var jwt_username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(jwt_username, out var userId)) 
                return Unauthorized();
            
            var user = _db.Users.AsNoTracking().FirstOrDefault(x => x.Id == userId);
            if (user is null) 
                return NotFound();
            
            return Ok(new { id = user.Id, username = user.Username, displayName = user.DisplayName, mustChangePassword = user.MustChangePassword });
        }

        /// <summary>
        /// Update current user's profile (display name and/or password)
        /// </summary>
        [HttpPatch]
        [Route("/v1/users/me")]
        public IActionResult UpdateProfile([FromBody] UserProfileUpdateRequest req)
        {
            var jwt_username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(jwt_username, out var userId)) 
                return Unauthorized();
            
            var user = _db.Users.FirstOrDefault(x => x.Id == userId);
            if (user is null) 
                return NotFound();

            if (!string.IsNullOrWhiteSpace(req.DisplayName)) 
                user.DisplayName = req.DisplayName!;

            if (!string.IsNullOrWhiteSpace(req.Password))
            {
                // require current password for security
                if (string.IsNullOrWhiteSpace(req.CurrentPassword)) 
                    return BadRequest("Current password is required to change password.");
                if (!PasswordHasher.Verify(req.CurrentPassword!, user.Password))
                    return BadRequest("Current password is incorrect.");

                // apply complexity rules
                var errors = PasswordValidator.Validate(req.Password!);
                if (errors.Count > 0)
                    return BadRequest(new { errors });

                user.Password = PasswordHasher.Hash(req.Password!);
                user.MustChangePassword = false;
            }

            _db.SaveChanges();
            return Ok(new { id = user.Id });
        }
    }
}
