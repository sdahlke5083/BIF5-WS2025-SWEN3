using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.DAL.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public sealed class Role
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!;

        // EF Core references
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

}
