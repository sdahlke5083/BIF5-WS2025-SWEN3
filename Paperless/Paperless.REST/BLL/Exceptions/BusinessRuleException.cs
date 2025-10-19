namespace Paperless.REST.BLL.Exceptions
{
    /// <summary>Business-Regel verletzt (z.B. Statuswechsel nicht erlaubt)</summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }
}
