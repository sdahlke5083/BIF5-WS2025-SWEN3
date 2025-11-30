namespace Paperless.REST.BLL.Models
{
    public class RabbitMqOptions
    {
        public string ServerAddress { get; set; } = "paperless-rabbitmq";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "paperless";
        public string Password { get; set; } = "paperless";
        public string QueueName { get; set; } = "ocr-queue";
        public bool Durable { get; set; } = true;
    }
}
