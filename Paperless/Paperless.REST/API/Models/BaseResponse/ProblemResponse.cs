namespace Paperless.REST.API.Models.BaseResponse
{
    /// <summary>
    /// Represents a standardized error response according to RFC 7807 (Problem Details for HTTP APIs).
    /// </summary>
    public class ProblemResponse
    {
        /// <summary>
        /// A URI that identifies the type of the problem.
        /// </summary>
        public string Type { get; set; } = null!;

        /// <summary>
        /// A short, human-readable summary of the problem.
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// The HTTP status code associated with this problem.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// A detailed human-readable description of the problem.
        /// </summary>
        public string Detail { get; set; } = null!;

        /// <summary>
        /// A URI that refers to the specific instance of the problem.
        /// </summary>
        public string Instance { get; set; } = null!;

        /// <summary>
        /// The trace ID that can be used to track the request.
        /// </summary>
        public string TraceId { get; set; } = null!;
    }
}
