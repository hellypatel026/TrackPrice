using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrackPrice.Models;

namespace TrackPrice.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        public DbSet<Store> Stores { get; set; }

        public DbSet<ProductListing> ProductListings { get; set; }

        public DbSet<PriceHistory> PriceHistories { get; set; }

        public DbSet<Watchlist> Watchlists { get; set; }

        public DbSet<PriceAlert> PriceAlerts { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductListing>()
                .Property(p => p.CurrentPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PriceHistory>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PriceAlert>()
                .Property(p => p.TargetPrice)
                .HasPrecision(18, 2);
        }
    }
}