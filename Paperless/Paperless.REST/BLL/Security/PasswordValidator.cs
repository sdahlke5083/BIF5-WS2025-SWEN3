using System.Text.RegularExpressions;

namespace Paperless.REST.BLL.Security
{
    /// <summary>
    /// Provides methods for validating passwords against common security requirements.
    /// </summary>
    /// <remarks>The <see cref="PasswordValidator"/> class offers static methods to check whether a password
    /// meets standard complexity rules, such as minimum length and character composition. This class cannot be
    /// instantiated.</remarks>
    public static class PasswordValidator
    {
        /// <summary>
        /// Validates the specified password against a set of complexity requirements.
        /// </summary>
        /// <remarks>The password is validated to ensure it is at least 8 characters long and contains at
        /// least one uppercase letter, one lowercase letter, one digit, and one special character (including symbols or
        /// underscore).</remarks>
        /// <param name="password">The password string to validate. Must not be <see langword="null"/> or empty.</param>
        /// <returns>A read-only list of error messages describing any validation failures. The list is empty if the password
        /// meets all requirements.</returns>
        public static IReadOnlyList<string> Validate(string password)
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(password))
            {
                errors.Add("Password must not be empty.");
                return errors;
            }

            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters long.");

            if (!Regex.IsMatch(password, "[A-Z]"))
                errors.Add("Password must contain at least one uppercase letter.");

            if (!Regex.IsMatch(password, "[a-z]"))
                errors.Add("Password must contain at least one lowercase letter.");

            if (!Regex.IsMatch(password, "[0-9]"))
                errors.Add("Password must contain at least one digit.");

            if (!Regex.IsMatch(password, "[\\W_]"))
                errors.Add("Password must contain at least one special character.");

            return errors;
        }
    }
}
