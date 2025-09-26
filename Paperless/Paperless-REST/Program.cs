using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
// Add services to the container.
services.AddControllers();
// Add Services for Swagger UI
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("Paperless", new OpenApiInfo
    {
        Version = "1.0.0",
        Title = "Paperless Dahlke Rašo",
        Description = "This is the specification of the required UI-facing REST API for the Document Management System of SWEN3 WS2025/26.\r\n " +
        "API is used by the Web UI to upload, process, search, share, and manage documents.\r\n " +
        "Internal workers (OCR, GenAI summary, ...) run in separate containers and are not directly exposed here.\r\n " +
        "Optional/nice-to-have features are marked via `x-optional: true` or may return 501 (Not Implemented) until implemented.\r\n\r\n " +
        "## Key decisions\r\n " +
        "- Runtime: .NET 9 + ASP.NET\r\n " +
        "- Consumers: Internal web UI (future external UIs possible)\r\n " +
        "- Auth: JWT Bearer for users; API Key for workers (if required); PSK (token + password) for guest access\r\n " +
        "- Uploads: multiple files supported (Multipart)\r\n " +
        "- Processing pipeline (asynchronous): upload ? OCR ? GenAI summary ? Indexing\r\n " +
        "- Search: free-text + filters; page-based pagination (page/pageSize with 10, 20, 50; default 20, max 100)\r\n " +
        "- Soft-delete with recycle bin; optional hard purge\r\n " +
        "- Workspaces for scoping; sharing via password-protected links\r\n " +
        "- Approvals feature `x-optional:true` (return 501 until implemented)\r\n " +
        "- Problem details: following RFC7807 with `traceId`",
        Contact = new OpenApiContact
        {
            Name = "Team G: Dahlke, Rašo",
            Email = "if23b234@technikum-wien.at",
            Url = new Uri("https://localhost/")
        },

    });
    options.AddServer(new OpenApiServer
    {
        Description = "Local development (HTTPS/TLS)",
        Url = "https://localhost:8081/",
    });
});


var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Add the SwaggerUI for development 
    app.UseStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI( options =>
    {
        options.SwaggerEndpoint("/swagger/Paperless/swagger.json", "Paperless-REST");
        options.RoutePrefix = string.Empty;
        options.InjectStylesheet("/assets/css/fhtw-swagger.css");
    });
}

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();
app.MapSwagger();

app.Run();
