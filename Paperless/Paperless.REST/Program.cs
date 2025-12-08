using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NLog.Web;
using Paperless.REST.API.Middleware;
using Paperless.REST.BLL.Models;
using Paperless.REST.BLL.Search;
using Paperless.REST.BLL.Storage;
using Paperless.REST.BLL.Uploads;
using Paperless.REST.BLL.Worker;
using Paperless.REST.DAL;
using Paperless.REST.DAL.DbContexts;
using Paperless.REST.DAL.Repositories;
using System.Reflection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.UseNLog();

var services = builder.Services;

// Configure RabbitMQ options from environment variables (fallback to sensible defaults)
services.Configure<RabbitMqOptions>(opts =>
{
    opts.ServerAddress = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "paperless-rabbitmq";
    opts.Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var p) ? p : 5672;
    opts.UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "paperless";
    opts.Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "paperless";
    // Use the configured exchange name for routing
    opts.ExchangeName = Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE") ?? "tasks";
    opts.ExchangeType = Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE_TYPE") ?? "direct";
    opts.Durable = true;
});

// MinIO Storage configuration
services.Configure<MinioStorageOptions>(
    builder.Configuration
        .GetSection("Paperless")
        .GetSection("Storage")
        .GetSection("Minio"));

services.AddSingleton<IFileStorageService, MinioFileStorageService>();


// Add services to the container.
services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

// Register HttpClient factory for Elasticsearch/worker calls
services.AddHttpClient();

// Register MyElasticSearchClient as singleton using the official Elasticsearch .NET client
services.AddSingleton<MyElasticSearchClient>();


services.AddDbContext<PostgressDbContext>(o =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"));
    o.UseCamelCaseNamingConvention();
});
services.AddSingleton<RabbitMqConnection>();
services.AddSingleton<IRabbitMqConnection>(sp => sp.GetRequiredService<RabbitMqConnection>());
services.AddHostedService(sp => sp.GetRequiredService<RabbitMqConnection>());
services.AddSingleton<DocumentEventPublisher>();
services.AddSingleton<IDocumentEventPublisher>(sp => sp.GetRequiredService<DocumentEventPublisher>());


// Add the Upload Service
services.AddScoped<IUploadService>(sp =>
{
    var repo = sp.GetRequiredService<IDocumentRepository>();
    var config = sp.GetRequiredService<IConfiguration>();
    var service = new UploadService(repo);
    service.Path = config.GetSection("Paperless").GetSection("Path").Value ?? "/.data/Files"; //TODO: fix this
    return service;
});

// Add the repositories
services.AddScoped<IDocumentRepository, DocumentRepository>();

// Add Development Services
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    // Add some custom settings
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
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,$"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Add the SwaggerUI for development 
    app.UseStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/Paperless/swagger.json", "Paperless");
        options.RoutePrefix = string.Empty;
        options.InjectStylesheet("/assets/css/fhtw-swagger.css");
    });

    app.ApplyMigrations();
}

//app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();
app.MapSwagger();

app.Run();
