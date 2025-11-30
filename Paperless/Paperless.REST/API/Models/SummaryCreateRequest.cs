using System.ComponentModel.DataAnnotations;

namespace Paperless.REST.API.Models
{
    public class SummaryCreateRequest
    {
        /// <summary>
        /// Optional: model used to create the summary (e.g. gpt-4)
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Optional: which preset to use for summary length/format. If not provided server will pick a default.
        /// </summary>
        public Guid? LengthPresetId { get; set; }

        /// <summary>
        /// The actual summary content produced by GenAI
        /// </summary>
        [Required]
        public string Content { get; set; } = default!;
    }
}
