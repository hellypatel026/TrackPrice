using System.ComponentModel.DataAnnotations;

namespace TrackPrice.Models
{
    public class ProductListing
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int StoreId { get; set; }

        [Required]
        public string ProductUrl { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal CurrentPrice { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Product Product { get; set; } = null!;

        public Store Store { get; set; } = null!;

        public ICollection<PriceHistory> PriceHistories { get; set; }
            = new List<PriceHistory>();
    }
}