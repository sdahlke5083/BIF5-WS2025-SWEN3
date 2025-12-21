using Paperless.UI.Components;

namespace Paperless.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Configure HttpClient for REST API
            var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://paperless.rest:8080";
            builder.Services.AddHttpClient<Services.IUploadsApiClient, Services.UploadsApiClient>(client =>
            {
                if (!apiBase.EndsWith("/")) apiBase += "/";
                client.BaseAddress = new Uri(apiBase);
            });

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Only use HTTPS redirection in production
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            else
            {
                // For development, you can log API connection info
                Console.WriteLine($"API Base URL configured as: {apiBase}");
            }

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
