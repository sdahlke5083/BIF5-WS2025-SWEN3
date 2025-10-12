namespace Paperless.REST.API.Models.BaseResponse
{
    /// <summary>
    /// Repräsentiert eine standardisierte Fehlerantwort nach RFC 7807 (Problem Details for HTTP APIs).
    /// </summary>
    public class ProblemResponse
    {
        /// <summary>
        /// Ein URI, der den Typ des Problems identifiziert.
        /// </summary>
        public string Type { get; set; } = null!;

        /// <summary>
        /// Eine kurze, menschenlesbare Zusammenfassung des Problems.
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// Der HTTP-Statuscode, der diesem Problem zugeordnet ist.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Eine detaillierte menschenlesbare Beschreibung des Problems.
        /// </summary>
        public string Detail { get; set; } = null!;

        /// <summary>
        /// Ein URI, der auf die spezifische Instanz des Problems verweist.
        /// </summary>
        public string Instance { get; set; } = null!;

        /// <summary>
        /// Die Trace-ID, die zur Nachverfolgung der Anfrage verwendet werden kann.
        /// </summary>
        public string TraceId { get; set; } = null!;
    }
}
