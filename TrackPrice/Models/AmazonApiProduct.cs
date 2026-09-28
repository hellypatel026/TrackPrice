namespace TrackPrice.Models
{
    public class AmazonApiProduct
    {
        public string Asin { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal? ListPrice { get; set; }

        public string Currency { get; set; } = "₹";

        public decimal Rating { get; set; }

        public int RatingCount { get; set; }

        public string Image { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Availability { get; set; } = string.Empty;

        public bool InStock { get; set; }
    }
}