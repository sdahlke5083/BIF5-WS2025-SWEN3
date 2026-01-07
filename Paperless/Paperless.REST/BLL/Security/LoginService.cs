using Microsoft.IdentityModel.Tokens;
using NLog;
using Paperless.REST.DAL.DbContexts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Paperless.REST.BLL.Security
{
    /// <summary>
    /// Login Service
    /// </summary>
    public class LoginService : ILoginService
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _config;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginService"/> class using the specified database context and
        /// configuration settings.
        /// </summary>
        /// <param name="userService">The user service used to validate user credentials. Cannot be <c>null</c>.</param>
        /// <param name="config">The application configuration settings used for authentication options and related parameters. Cannot be
        /// <c>null</c>.</param>
        public LoginService(IUserService userService, IConfiguration config)
        {
            _userService = userService;
            _config = config;
        }

        /// <summary>
        /// Authenticates a user with the specified username and password, and returns a JSON Web Token (JWT) if
        /// authentication is successful.
        /// </summary>
        /// <remarks>The returned JWT can be used to authorize subsequent requests on behalf of the
        /// authenticated user.</remarks>
        /// <param name="username">The username of the user to authenticate. Cannot be null, empty, or consist only of white-space characters.</param>
        /// <param name="password">The password associated with the specified username. Cannot be null, empty, or consist only of white-space
        /// characters.</param>
        /// <returns>A JWT as a string if the credentials are valid; otherwise, <see langword="null"/>.</returns>
        public string? Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = _userService.ValidateCredentials(username, password);
            if (user is null) return null;

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

            return tokenString;
        }
    }
}
