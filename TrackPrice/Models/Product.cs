using System.ComponentModel.DataAnnotations;

namespace TrackPrice.Models
{
    public class Product
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string? ExternalProductId { get; set; }

        [StringLength(50)]
        public string? Source { get; set; }

        // Flipkart identifiers used for price checking
        public string? ProductUrl { get; set; }

        [StringLength(100)]
        public string? ItemId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<ProductListing> ProductListings { get; set; }
            = new List<ProductListing>();

        public ICollection<Watchlist> Watchlists { get; set; }
            = new List<Watchlist>();

        public ICollection<PriceAlert> PriceAlerts { get; set; }
            = new List<PriceAlert>();
    }
}