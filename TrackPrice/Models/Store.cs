using System.ComponentModel.DataAnnotations;

namespace TrackPrice.Models
{
    public class Store
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? WebsiteUrl { get; set; }

        public ICollection<ProductListing> ProductListings { get; set; }
            = new List<ProductListing>();
    }
}