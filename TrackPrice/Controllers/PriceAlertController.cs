using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrackPrice.Data;
using TrackPrice.Models;
using TrackPrice.Services;
namespace TrackPrice.Controllers
{
    [Authorize]
    public class PriceAlertController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReefApiService _reefApiService;

        public PriceAlertController(
            ApplicationDbContext context,
            ReefApiService reefApiService)
        {
            _context = context;
            _reefApiService = reefApiService;
        }

        // Show user's price alerts
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge();
            }

            var alerts = await _context.PriceAlerts
                .Include(a => a.Product)
                .ThenInclude(p => p.ProductListings)
                .ThenInclude(pl => pl.Store)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(alerts);
        }

        // Create a price alert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    int productId,
    decimal targetPrice,
    string url,
    string itmId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge();
            }

            // Target price must be greater than zero
            if (targetPrice <= 0)
            {
                TempData["PriceAlertError"] =
                    "Please enter a valid target price.";

                return RedirectToAction(
    "Details",
    "Products",
    new
    {
        url = url,
        itmId = itmId
    });
            }

            // Check whether product exists
            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
            {
                return NotFound();
            }
            // Get the latest Flipkart price
            var flipkartProduct =
                await _reefApiService.GetFlipkartProductAsync(
                    url,
                    itmId);

            if (flipkartProduct == null)
            {
                TempData["PriceAlertError"] =
                    "Unable to get the current product price.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        url = url,
                        itmId = itmId
                    });
            }

            decimal currentPrice = flipkartProduct.Price;

            // Check whether the user already has
            // an active alert for this product
            var existingAlert = await _context.PriceAlerts
                .FirstOrDefaultAsync(a =>
                    a.UserId == userId &&
                    a.ProductId == productId &&
                    a.IsActive);

            if (existingAlert != null)
            {
                existingAlert.TargetPrice = targetPrice;
                existingAlert.CurrentPrice = currentPrice;
                existingAlert.TriggeredAt = null;
                existingAlert.IsActive = true;
            }
            else
            {
                var alert = new PriceAlert
                {
                    UserId = userId,
                    ProductId = productId,
                    TargetPrice = targetPrice,

                    // Current price can be updated later
                    // by the price tracking service.
                    CurrentPrice = currentPrice,

                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    TriggeredAt = null
                };

                _context.PriceAlerts.Add(alert);
            }



            await _context.SaveChangesAsync();

            TempData["PriceAlertSuccess"] =
                "Price alert has been created successfully.";

            return RedirectToAction(
    "Details",
    "Products",
    new
    {
        url = url,
        itmId = itmId
    });
        }
        // Deactivate a price alert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge();
            }

            var alert = await _context.PriceAlerts
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (alert == null)
            {
                return NotFound();
            }

            alert.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["PriceAlertSuccess"] =
                "Price alert has been deactivated.";

            return RedirectToAction(nameof(Index));
        }
    }
}