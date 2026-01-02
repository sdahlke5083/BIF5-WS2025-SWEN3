using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    [Index(nameof(Username),IsUnique = true)]
    public sealed class User
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(128)]
        public string Username { get; set; } = default!;
        [Required]
        [MaxLength(256)]
        public string DisplayName { get; set; } = default!;
        [Required]
        public string Password { get; set; } = default!;
        // flag to indicate password must be changed on first login
        public bool MustChangePassword { get; set; } = false;
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset CreatedAt { get; set; }

        // EF Core references
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
