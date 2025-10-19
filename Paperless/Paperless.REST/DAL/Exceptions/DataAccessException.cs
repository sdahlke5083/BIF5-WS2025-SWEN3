using System;

namespace Paperless.REST.DAL.Exceptions
{
    /// <summary>
    /// Base exception for all data access layer exceptions
    /// </summary>
    public class DataAccessException : Exception
    {
        public DataAccessException(string message) : base(message) { }
        
        public DataAccessException(string message, Exception innerException) : base(message, innerException) { }
    }
}
