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

        public async Task PublishDocumentUploadedAsync(Guid documentId, CancellationToken ct = default)
        {
            var _channel = await _rabbitMqConnection.CreateChannelAsync(ct);

            var message = new
            {
                schema = "paperless.document_uploaded.v1",
                document_id = documentId
            };
            var messageBody = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queueOptions.QueueName,
                body: messageBody);
            _logger.Debug($"Published document_uploaded event for document ID {documentId} to RabbitMQ.");

        }
    }
}
