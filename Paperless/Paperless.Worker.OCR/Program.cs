using Paperless.Worker.OCR;
using Paperless.Worker.OCR.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<DemoConsumer>();

var host = builder.Build();
host.Run();
