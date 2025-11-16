using Paperless.Worker.OCR;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OcrWorker>();
//builder.Services.AddHostedService<DemoConsumer>();

var host = builder.Build();
host.Run();
