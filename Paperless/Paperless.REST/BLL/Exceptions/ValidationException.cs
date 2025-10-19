namespace Paperless.REST.BLL.Exceptions
{
    /// <summary>Business-Validierung fehlgeschlagen (fehlende/falsche Eingaben etc.)</summary>
    public class ValidationException : Exception
    {
        public IReadOnlyCollection<string> Errors { get; }

        public ValidationException(string message, IEnumerable<string>? errors = null)
            : base(message)
        {
            Errors = (errors ?? Array.Empty<string>()).ToArray();
        }
    }
}
