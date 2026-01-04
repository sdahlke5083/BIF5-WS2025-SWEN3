using System.Threading;
using System.Threading.Tasks;

namespace Paperless.REST.BLL.Diagnostics
{
    /// <summary>
    /// Defines a service for retrieving runtime diagnostics information about the application and its environment.
    /// </summary>
    /// <remarks>Implementations of this interface provide methods to collect diagnostic data such as
    /// application version, database version, queue backlog, and worker connectivity. This information can be used for
    /// monitoring, troubleshooting, or reporting the health of the system.</remarks>
    public interface IDiagnosticsService
    {
        /// <summary>
        /// Gathers runtime diagnostics information (application version, database version, queue backlog, worker connectivity).
        /// </summary>
        Task<DiagnosticsInfo> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
    }
}
