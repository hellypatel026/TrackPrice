using TrackPrice.Models;

namespace TrackPrice.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Don't add sample data if products already exist
            if (context.Products.Any())
            {
                return;
            }

            var store = new Store
            {
                Name = "Flipkart",
                WebsiteUrl = "https://www.flipkart.com"
            };

            var product = new Product
            {
                Name = "Sample Smartphone",
                Description = "Sample product for testing TrackPrice.",
                Category = "Mobiles",
                ImageUrl = null
            };

            context.Stores.Add(store);
            context.Products.Add(product);

            await context.SaveChangesAsync();

            var listing = new ProductListing
            {
                ProductId = product.Id,
                StoreId = store.Id,
                ProductUrl = "https://www.flipkart.com",
                CurrentPrice = 19999,
                IsAvailable = true
            };

            context.ProductListings.Add(listing);

            await context.SaveChangesAsync();

            var priceHistory = new PriceHistory
            {
                ProductListingId = listing.Id,
                Price = 19999
            };

            context.PriceHistories.Add(priceHistory);

            await context.SaveChangesAsync();
        }
    }
}