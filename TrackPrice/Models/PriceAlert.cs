using System.ComponentModel.DataAnnotations;

namespace TrackPrice.Models
{
    public class PriceAlert
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int ProductId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TargetPrice { get; set; }

        public bool IsTriggered { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Product Product { get; set; } = null!;
    }
}