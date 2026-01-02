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
            var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://paperless.rest:8081";
            if (!apiBase.EndsWith("/")) apiBase += "/";

            // Protected browser storage for token persistence (Server interactive components)
            builder.Services.AddServerSideBlazor();

            // plain client for auth (no auth header)
            builder.Services.AddHttpClient("auth", client => client.BaseAddress = new Uri(apiBase));

            // register AuthService using the plain client and protected storage
            builder.Services.AddScoped<Services.IAuthService>(sp =>
            {
                var factory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
                var client = factory.CreateClient("auth");
                var js = sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
                return new Services.AuthService(client, js);
            });

            // register DelegatingHandler to attach token
            builder.Services.AddTransient<Services.AuthMessageHandler>();

            // named client for API calls that will have auth header attached
            builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri(apiBase))
                .AddHttpMessageHandler<Services.AuthMessageHandler>();

            // typed API clients using the authenticated named client
            builder.Services.AddHttpClient<Services.IUploadsApiClient, Services.UploadsApiClient>(client => client.BaseAddress = new Uri(apiBase))
                .AddHttpMessageHandler<Services.AuthMessageHandler>();
            builder.Services.AddHttpClient<Services.IWorkspacesApiClient, Services.WorkspacesApiClient>(client => client.BaseAddress = new Uri(apiBase))
                .AddHttpMessageHandler<Services.AuthMessageHandler>();
            builder.Services.AddHttpClient<Services.IUsersApiClient, Services.UsersApiClient>(client => client.BaseAddress = new Uri(apiBase))
                .AddHttpMessageHandler<Services.AuthMessageHandler>();

            builder.Services.AddSingleton<Services.INotificationService, Services.NotificationService>();
            builder.Services.AddScoped<Services.IThemeService, Services.ThemeService>();

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
