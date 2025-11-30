namespace Paperless.Worker.GenAI.Exceptions
{
    /// <summary>
    /// Fehler bei der Kommunikation mit der GenAI-API
    /// </summary>
    public class GenAiApiException : Exception
    {
        public GenAiApiException(string message, Exception? inner = null)
            : base(message, inner)
        {
        }
    }
}
