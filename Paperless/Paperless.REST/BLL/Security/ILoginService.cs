namespace Paperless.REST.BLL.Security
{
    /// <summary>
    /// Defines a service for authenticating users and generating authentication tokens.
    /// </summary>
    /// <remarks>Implementations of this interface provide methods to verify user credentials and issue tokens
    /// for authenticated sessions. Typically used to support login functionality in applications requiring user
    /// authentication.</remarks>
    public interface ILoginService
    {
        /// <summary>
        /// Authenticates a user by username and password and returns a JWT token string on success, otherwise null.
        /// </summary>
        string? Authenticate(string username, string password);
    }
}
