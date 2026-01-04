namespace Paperless.REST.BLL.Diagnostics
{
    public sealed class DiagnosticsInfo
    {
        public string ApplicationVersion { get; set; } = string.Empty;
        public string DatabaseVersion { get; set; } = string.Empty;
        public int QueueBacklog { get; set; }
        public bool WorkersConnected { get; set; }
    }
}
