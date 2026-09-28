namespace TrackPrice.Models
{
    public class ReefApiProduct
    {
        public string ProductId { get; set; } = string.Empty;

        public string ListingId { get; set; } = string.Empty;

        public string ItemId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Subtitle { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal? Mrp { get; set; }

        public decimal DiscountPercent { get; set; }

        public string Currency { get; set; } = "INR";

        public decimal Rating { get; set; }

        public int RatingCount { get; set; }

        public string Image { get; set; } = string.Empty;

        public string Availability { get; set; } = string.Empty;

        public bool InStock { get; set; }

        public string Category { get; set; } = string.Empty;
    }
}