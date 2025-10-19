namespace Paperless.REST.BLL.Models
{
    public class RabbitMqOptions
    {
        public string ServerAddress { get; set; } = "localhost";
        public string QueueName { get; set; } = "demo";
        public bool Durable { get; set; } = true;
    }
}
