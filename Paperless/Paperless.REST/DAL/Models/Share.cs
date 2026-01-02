using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    public sealed class Share
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Token { get; set; } = default!; // public token string for guest link

        [MaxLength(256)]
        public string? PasswordHash { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset CreatedAt { get; set; }

        // EF reference
        public Document Document { get; set; } = default!;
    }
}
