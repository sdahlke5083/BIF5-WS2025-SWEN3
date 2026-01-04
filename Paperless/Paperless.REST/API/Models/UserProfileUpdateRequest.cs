using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Models
{
    public sealed class UserProfileUpdateRequest
    {
        [MaxLength(256)]
        public string? DisplayName { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }

        [MinLength(6)]
        public string? CurrentPassword { get; set; }
    }
}
