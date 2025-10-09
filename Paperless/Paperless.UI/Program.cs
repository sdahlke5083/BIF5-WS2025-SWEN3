using System;
using System.IO;
using Paperless.UI.Components;

namespace Paperless.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Ensure the hosting content root used to initialize static web assets is absolute.
            // Try a couple of reasonable defaults (current working dir and AppContext.BaseDirectory).
            WebApplicationBuilder builder = null;
            var candidateRoots = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };

            foreach (var candidate in candidateRoots)
            {
                try
                {
                    var options = new WebApplicationOptions
                    {
                        Args = args,
                        ContentRootPath = Path.GetFullPath(candidate)
                    };

                    builder = WebApplication.CreateBuilder(options);
                    break;
                }
                catch (ArgumentException ex) when (ex.ParamName == "root" || ex.Message.Contains("root"))
                {
                    // Manifest or environment provided a non-absolute root; try next candidate.
                }
            }

            if (builder == null)
            {
                // If none of the candidates worked, surface a clear failure to help debugging.
                throw new InvalidOperationException("Failed to create WebApplicationBuilder with an absolute content root. Inspect static web assets manifest and project configuration.");
            }

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}