using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Paperless.REST.DAL.Models
{
    public sealed class ProcessingStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid DocumentId { get; set; }
        public ProcessingState Ocr { get; set; } = ProcessingState.NotProcessed;
        public ProcessingState Summary { get; set; } = ProcessingState.NotProcessed;
        public ProcessingState Index { get; set; } = ProcessingState.NotProcessed;
        public string? LastError { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset? UpdatedAt { get; set; }

        // EF Core reference
        public Document Document { get; set; } = default!;
    }

}
