using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Models.QueryModels
{
    /// <summary>
    /// Parameters for querying audit logs.
    /// </summary>
    public class AuditRequestQuery
    {
        /// <summary>
        /// the logging level for the request.
        /// </summary>
        /// <remarks><i>Available values:</i> minimal, normal, excessive</remarks>
        public string Level { get; set; } = "minimal"; // Default to "minimal" if not provided

        /// <summary>
        /// the current page number for pagination.
        /// </summary>
        /// <remarks><i>Default value:</i> 1</remarks>
        public int Page { get; set; } = 1;          // Default to page 1 if not provided

        /// <summary>
        /// number of items per page for pagination (max 100).
        /// </summary>
        /// <remarks>Number of items per page. <br/> <i>Default value:</i> 20<br/><i>Maximum value:</i> 100 (max. supported server-side)</remarks>
        [Range(5, 100, ErrorMessage = "PageSize must be between 5 and 100.")]
        public int PageSize { get; set; } = 20;     // Default to 20 if not provided (default 20; max 100 supported server-side)
    }
}
