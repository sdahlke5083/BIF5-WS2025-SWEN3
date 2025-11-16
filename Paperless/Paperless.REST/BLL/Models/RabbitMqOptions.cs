namespace Paperless.REST.BLL.Models
{
    public class RabbitMqOptions
    {
        public string ServerAddress { get; set; } = "paperless-rabbitmq";
        public string QueueName { get; set; } = "demo";
        public bool Durable { get; set; } = true;
    }
}
