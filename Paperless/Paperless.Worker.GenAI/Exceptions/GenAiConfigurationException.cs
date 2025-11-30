namespace Paperless.Worker.GenAI.Exceptions
{
    /// <summary>
    /// Fehler in der Konfiguration des GenAI-Workers
    /// </summary>
    public class GenAiConfigurationException : Exception
    {
        public GenAiConfigurationException(string message)
            : base(message)
        {
        }
    }
}
