namespace Paperless.REST.DAL.Models
{
    public enum ProcessingState
    {
        NotProcessed,
        Queued,
        Running,
        Succeeded,
        Failed
    }
}
