using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Paperless.REST.BLL.Security
{
    public static class PasswordValidator
    {
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
