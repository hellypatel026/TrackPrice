using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrackPrice.Data;
using TrackPrice.Models;

namespace TrackPrice.Services
{
    public class PriceAlertBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PriceAlertBackgroundService> _logger;
        

        public PriceAlertBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<PriceAlertBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "========== PRICE ALERT SERVICE STARTED ==========");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckPriceAlertsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error while checking price alerts.");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(10),
                    stoppingToken);
            }
        }

        private async Task CheckPriceAlertsAsync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            var reefApiService =
                scope.ServiceProvider
                    .GetRequiredService<ReefApiService>();
            var emailNotificationService =
    scope.ServiceProvider
        .GetRequiredService<EmailNotificationService>();

            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<UserManager<ApplicationUser>>();

            var alerts = await context.PriceAlerts
                .Include(a => a.Product)
                .Where(a =>
                    a.IsActive &&
                    a.Product != null &&
                    a.Product.ProductUrl != null &&
                    a.Product.ItemId != null)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Checking {Count} active price alerts.",
                alerts.Count);

            foreach (var alert in alerts)
            {
                if (string.IsNullOrWhiteSpace(
                        alert.Product!.ProductUrl) ||
                    string.IsNullOrWhiteSpace(
                        alert.Product.ItemId))
                {
                    continue;
                }

                try
                {
                    var product =
                        await reefApiService.GetFlipkartProductAsync(
                            alert.Product.ProductUrl,
                            alert.Product.ItemId);

                    if (product == null || product.Price <= 0)
                    {
                        _logger.LogWarning(
                            "Could not get a valid price for alert {AlertId}.",
                            alert.Id);

                        continue;
                    }

                    var currentPrice = product.Price;

                    // Update the current price stored in the alert
                    alert.CurrentPrice = currentPrice;

                    // Find the Flipkart store
                    var flipkartStore =
                        await context.Stores
                            .FirstOrDefaultAsync(
                                s => s.Name == "Flipkart",
                                cancellationToken);

                    if (flipkartStore == null)
                    {
                        _logger.LogWarning(
                            "Flipkart store was not found.");

                        continue;
                    }

                    // Find the product listing
                    var listing =
                        await context.ProductListings
                            .FirstOrDefaultAsync(
                                pl =>
                                    pl.ProductId ==
                                        alert.ProductId &&
                                    pl.StoreId ==
                                        flipkartStore.Id,
                                cancellationToken);

                    // Create the listing if it does not exist
                    if (listing == null)
                    {
                        listing = new ProductListing
                        {
                            ProductId = alert.ProductId,
                            StoreId = flipkartStore.Id,
                            ProductUrl =
                                alert.Product.ProductUrl,
                            CurrentPrice = currentPrice,
                            IsAvailable = true,
                            LastUpdated = DateTime.UtcNow
                        };

                        context.ProductListings.Add(listing);

                        await context.SaveChangesAsync(
                            cancellationToken);

                        _logger.LogInformation(
                            "Created Flipkart listing for product {ProductId}.",
                            alert.ProductId);
                    }
                    else
                    {
                        listing.CurrentPrice = currentPrice;
                        listing.IsAvailable = true;
                        listing.LastUpdated = DateTime.UtcNow;
                    }

                    // Record price history
                    // Record price history only when the price changes
                    var lastHistory = await context.PriceHistories
                        .Where(h => h.ProductListingId == listing.Id)
                        .OrderByDescending(h => h.RecordedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (lastHistory == null ||
                        lastHistory.Price != currentPrice)
                    {
                        var history = new PriceHistory
                        {
                            ProductListingId = listing.Id,
                            Price = currentPrice,
                            RecordedAt = DateTime.UtcNow
                        };

                        context.PriceHistories.Add(history);

                        _logger.LogInformation(
                            "Price history recorded for product {ProductId}. " +
                            "Price: {CurrentPrice}.",
                            alert.ProductId,
                            currentPrice);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Price unchanged for product {ProductId}. " +
                            "No new price history record created.",
                            alert.ProductId);
                    }
                    // Trigger the alert when target price is reached
                    if (currentPrice <= alert.TargetPrice)
                    {
                        var user =
                            await userManager.FindByIdAsync(alert.UserId);

                        if (user != null &&
                            !string.IsNullOrWhiteSpace(user.Email))
                        {
                            await emailNotificationService
                                .SendPriceAlertEmailAsync(
                                    user.Email,
                                    alert.Product.Name,
                                    alert.TargetPrice,
                                    currentPrice,
                                    alert.Product.ProductUrl);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Could not send price alert email for " +
                                "alert {AlertId}. User email was not found.",
                                alert.Id);
                        }

                        var notification = new Notification
                        {
                            UserId = alert.UserId,
                            Title = "Price Alert Triggered",
                            Message =
                                $"The price of {alert.Product.Name} " +
                                $"has reached your target price. " +
                                $"Current price: ₹{currentPrice:N2}. " +
                                $"Target price: ₹{alert.TargetPrice:N2}.",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow,
                            LinkUrl = alert.Product.ProductUrl
                        };

                        context.Notifications.Add(notification);

                        alert.IsActive = false;
                        alert.TriggeredAt = DateTime.UtcNow;

                        _logger.LogInformation(
                            "Price alert {AlertId} triggered. " +
                            "Current price: {CurrentPrice}, " +
                            "Target price: {TargetPrice}. " +
                            "In-app notification created.",
                            alert.Id,
                            currentPrice,
                            alert.TargetPrice);
                    }

                    await context.SaveChangesAsync(
                        cancellationToken);

                    _logger.LogInformation(
                        "Price alert {AlertId} checked successfully. " +
                        "Current price: {CurrentPrice}.",
                        alert.Id,
                        currentPrice);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error checking price alert {AlertId}.",
                        alert.Id);
                }
            }
        }
    }
}