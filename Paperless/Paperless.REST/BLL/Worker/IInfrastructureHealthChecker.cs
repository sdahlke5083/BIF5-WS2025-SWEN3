namespace Paperless.REST.BLL.Worker
{
    /// <summary>
    /// Schnittstelle für die BLL-Prüfung der externen Abhängigkeiten.
    /// Implementierung muss in der BLL liegen und in DI registriert werden.
    /// Methode gibt true zurück, wenn alle Abhängigkeiten bereit sind,
    /// sonst ein Array mit den Namen der nicht-bereiten Abhängigkeiten.
    /// </summary>
    public interface IInfrastructureHealthChecker
    {
        /// <summary>
        /// Asynchronously checks the readiness of all required dependencies.
        /// </summary>
        /// <returns>A tuple containing a boolean value that is <see langword="true"/> if all dependencies are ready; otherwise,
        /// <see langword="false"/>.  The tuple also includes an array of strings listing the names of dependencies that
        /// are not ready.  The array is empty if all dependencies are ready.</returns>
        Task<(bool AllReady, string[] NotReady)> CheckDependenciesAsync();
    }
}
