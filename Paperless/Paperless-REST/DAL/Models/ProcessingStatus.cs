namespace DAL.Models
{
    public sealed class ProcessingStatus
    {
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = default!;
        public ProcessingState Ocr { get; set; } = ProcessingState.NotProcessed;
        public ProcessingState Summary { get; set; } = ProcessingState.NotProcessed;
        public ProcessingState Index { get; set; } = ProcessingState.NotProcessed;
        public string? LastError { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

}
