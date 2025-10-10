using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    public sealed class DocumentTag
    {

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid DocumentId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid TagId { get; set; }

        // EF Core references
        public Document Document { get; set; } = default!;
        public Tag Tag { get; set; } = default!;
    }
}
