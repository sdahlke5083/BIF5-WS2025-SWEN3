using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paperless.REST.API.Models;
using Paperless.REST.BLL.Security;
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
        private readonly IUserService _userService;
        private readonly ILoginService _loginService;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class with the specified database context and
        /// configuration settings.
        /// </summary>
        /// <remarks>Use this constructor to provide the necessary dependencies for authentication
        /// functionality. All parameters must be non-null.</remarks>
        /// <param name="userService">The user service used to manage jwt_username profiles and data.</param>
        /// <param name="loginService">The login service used to authenticate users and issue tokens.</param>
        public AuthController(IUserService userService, ILoginService loginService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
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
            if (req is null) 
                return BadRequest();

            // Verwende constructor-injiziertes Login-Service statt RequestServices.GetService
            var tokenString = _loginService.Authenticate(req.Username, req.Password);
            if (tokenString is null)
                return Unauthorized();

            return Ok(new { token = tokenString });
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("/v1/users/me")]
        public IActionResult GetProfile()
        {
            var jwt_username = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(jwt_username, out var userId))
                return Unauthorized();

            var dto = _userService.GetProfile(userId);
            if (dto is null) return NotFound();

            return Ok(new { id = dto.Id, username = dto.Username, displayName = dto.DisplayName, mustChangePassword = dto.MustChangePassword });
        }

        /// <summary>
        /// Update current user's profile (display name and/or password)
        /// </summary>
        [HttpPatch]
        [Authorize]
        [Route("/v1/users/me")]
        public IActionResult UpdateProfile([FromBody] UserProfileUpdateRequest req)
        {
            var jwt_username = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(jwt_username, out var userId))
                return Unauthorized();

            var result = _userService.UpdateProfile(userId, req, requireCurrentPassword: true);
            if (!result.Success)
            {
                if (result.Errors?.Count > 0)
                    return BadRequest(new { errors = result.Errors });
                return NotFound();
            }

            return Ok(new { id = result.Id });
        }
    }
}
