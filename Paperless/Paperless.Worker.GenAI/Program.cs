using NLog;
using NLog.Extensions.Logging;

namespace Paperless.Worker.GenAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var logger = LogManager.Setup()
                                   .LoadConfigurationFromSection(builder.Configuration, "NLog")
                                   .GetCurrentClassLogger();

            try
            {
                logger.Debug("GenAI Worker init main");

                builder.Logging.ClearProviders();
                ILoggingBuilder loggingBuilder = builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.Logging.AddNLog();   // nutzt NLog-Config aus appsettings

                builder.Services.AddSingleton<Connectors.GenAiConnector>();
                builder.Services.AddHostedService<GenAiWorker>();

                var host = builder.Build();
                host.Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped GenAI Worker because of an exception");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}
