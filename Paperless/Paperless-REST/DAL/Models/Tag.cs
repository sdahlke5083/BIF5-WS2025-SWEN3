namespace Paperless.REST.DAL.Models
{
    public sealed class Tag
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

}
