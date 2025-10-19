namespace Paperless.REST.DAL.Exceptions
{
    /// <summary>Generische Datenzugriffs-Fehler</summary>
    public class DataAccessException : Exception
    {
        public DataAccessException(string message, Exception? inner = null) : base(message, inner) { }
    }
}
