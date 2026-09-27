using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrackPrice.Data;
using TrackPrice.Models;

namespace TrackPrice.Controllers
{
    [Authorize]
    public class PriceAlertController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PriceAlertController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show user's price alerts
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge();
            }

            var alerts = await _context.PriceAlerts
                .Include(a => a.Product)
                .ThenInclude(p => p.ProductListings)
                .ThenInclude(pl => pl.Store)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return View(alerts);
        }

        // Create a price alert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int productId,
            decimal targetPrice)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge();
            }

            if (targetPrice <= 0)
            {
                return RedirectToAction(
                    "Details",
                    "Products",
                    new { id = productId });
            }

            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
            {
                return NotFound();
            }

            var alert = new PriceAlert
            {
                UserId = userId,
                ProductId = productId,
                TargetPrice = targetPrice,
                IsTriggered = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.PriceAlerts.Add(alert);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "Products",
                new { id = productId });
        }
    }
}