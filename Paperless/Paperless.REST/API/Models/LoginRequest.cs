using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Models
{
    public sealed class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
