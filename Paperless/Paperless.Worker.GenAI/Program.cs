namespace Paperless.Worker.GenAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<GenAiWorker>();

            var host = builder.Build();
            host.Run();
        }
    }
}