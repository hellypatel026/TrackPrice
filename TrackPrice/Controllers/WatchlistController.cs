using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrackPrice.Data;
using TrackPrice.Models;

namespace TrackPrice.Controllers
{
    [Authorize]
    public class WatchlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WatchlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show user's watchlist
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge();
            }

            var watchlist = await _context.Watchlists
                .Include(w => w.Product)
                .ThenInclude(p => p.ProductListings)
                .ThenInclude(pl => pl.Store)
                .Where(w => w.UserId == userId)
                .ToListAsync();

            return View(watchlist);
        }

        // Add product to watchlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge();
            }

            // Check whether product exists
            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
            {
                return NotFound();
            }

            // Check if product is already in user's watchlist
            var alreadyExists = await _context.Watchlists
                .AnyAsync(w =>
                    w.UserId == userId &&
                    w.ProductId == productId);

            // Add only if it is not already there
            if (!alreadyExists)
            {
                var watchlistItem = new Watchlist
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow
                };

                _context.Watchlists.Add(watchlistItem);

                await _context.SaveChangesAsync();
            }

            // Return to product details
            return RedirectToAction(
                "Details",
                "Products",
                new { id = productId });
        }
    }
}