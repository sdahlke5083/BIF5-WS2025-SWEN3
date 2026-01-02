using System;

namespace Paperless.UI.Services
{
    public interface INotificationService
    {
        void Notify(string message, string level = "info");
        event Action<string, string>? OnNotify;
    }
}
