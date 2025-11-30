using Microsoft.Extensions.Options;
using Paperless.REST.BLL.Models;
using RabbitMQ.Client;
using System.Text.Json;

namespace Paperless.REST.BLL.Worker
{
    public class DocumentEventPublisher : IDocumentEventPublisher
    {
        private readonly IRabbitMqConnection _rabbitMqConnection;
        private readonly RabbitMqOptions _queueOptions;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public DocumentEventPublisher(IRabbitMqConnection rabbitMqConnection, IOptions<RabbitMqOptions> options)
        {
            _rabbitMqConnection = rabbitMqConnection;
            _queueOptions = options.Value;
        }

        public async Task PublishDocumentUploadedAsync(string documentId, CancellationToken ct = default)
        {
            var _channel = await _rabbitMqConnection.CreateChannelAsync(ct);

            // create a task message for OCR
            var message = new
            {
                schema = "paperless.task.v1",
                document_id = documentId
            };
            var messageBody = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

            // publish to configured exchange using routing key 'ocr'
            var exchange = _queueOptions.ExchangeName ?? "tasks";
            var routingKey = "ocr";

            await _channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                body: messageBody);

            _logger.Debug($"Published document_uploaded task for document ID {documentId} to exchange '{exchange}' with routingKey '{routingKey}'.");

        }
    }
}
