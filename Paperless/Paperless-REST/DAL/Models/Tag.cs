using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.DAL.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public sealed class Tag
    {
        [Key]
        [DefaultValue("gen_random_uuid()")]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(64)]
        public string Name { get; set; } = default!;

        // EF Core references
        public ICollection<DocumentTag> DocumentTags { get; set; } = new List<DocumentTag>();
    }

}
