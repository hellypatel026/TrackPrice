using System.ComponentModel.DataAnnotations;

namespace TrackPrice.Models
{
    public class PriceHistory
    {
        public int Id { get; set; }

        public int ProductListingId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ProductListing ProductListing { get; set; } = null!;
    }
}