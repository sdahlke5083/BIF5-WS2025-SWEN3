namespace Paperless.REST.DAL.Exceptions
{
    /// <summary>Domain-Entität wurde nicht gefunden</summary>
    public class EntityNotFoundException : Exception
    {
        public string? EntityName { get; }
        public object? Key { get; }

        public EntityNotFoundException(string message, string? entityName = null, object? key = null)
            : base(message)
        {
            EntityName = entityName;
            Key = key;
        }
    }
}