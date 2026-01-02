using System;

namespace Paperless.UI.Services
{
    public class NotificationService : INotificationService
    {
        public event Action<string, string>? OnNotify;

        public void Notify(string message, string level = "info")
        {
            try { OnNotify?.Invoke(message, level); } catch { }
        }
    }
}
