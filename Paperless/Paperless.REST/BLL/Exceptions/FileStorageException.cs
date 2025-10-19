namespace Paperless.REST.BLL.Exceptions
{
    /// <summary>
    /// Datei-/Speicherfehler auf BLL-Ebene (z.B. Persistierung fehlgeschlagen).
    /// Wird in der API zu 500 gemappt.
    /// </summary>
    public class FileStorageException : Exception
    {
        public FileStorageException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}
