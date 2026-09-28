namespace TrackPrice.Models
{
    public class ProductComparisonViewModel
    {
        public ReefApiProduct FlipkartProduct { get; set; } = null!;

        public List<AmazonApiProduct> AmazonProducts { get; set; }
            = new List<AmazonApiProduct>();
    }
}