namespace Paperless.REST.BLL.Models
{
    public class RabbitMqOptions
    {
        public string ServerAddress { get; set; } = "paperless-rabbitmq";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "paperless";
        public string Password { get; set; } = "paperless";
        // Exchange name for task routing
        public string ExchangeName { get; set; } = "tasks";
        public bool Durable { get; set; } = true;
        // Exchange type (direct/topic/headers) - default to direct for routingKey usage
        public string ExchangeType { get; set; } = "direct";
    }
}
