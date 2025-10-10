using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    public sealed class DocumentLanguage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string ISO639Code { get; set; } = default!; // e.g. de, en, es, fr, ...
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!; // e.g. Deutsch, Englisch, Spanisch, Französich, ...
    }
}
