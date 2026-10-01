using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrackPrice.Data;

namespace TrackPrice.Controllers
{
    [Authorize]
    public class PriceHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PriceHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int productId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var hasAccess = await _context.PriceAlerts
                .AnyAsync(a =>
                    a.ProductId == productId &&
                    a.UserId == userId);

            if (!hasAccess)
            {
                return Forbid();
            }

            var history = await _context.PriceHistories
                .Include(h => h.ProductListing)
                .ThenInclude(pl => pl.Store)
                .Where(h =>
                    h.ProductListing.ProductId == productId)
                .OrderByDescending(h => h.RecordedAt)
                .ToListAsync();

            ViewBag.Product = product;

            return View(history);
        }
    }
}