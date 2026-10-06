using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IActionResult> Index(int? productId)
        {
            if (!productId.HasValue)
            {
                return View(new PriceHistoryViewModel());
            }

            var product = await _context.Products
                .Include(p => p.ProductListings)
                    .ThenInclude(pl => pl.Store)
                .FirstOrDefaultAsync(p => p.Id == productId.Value);

            if (product == null)
            {
                return NotFound();
            }

            var listing = product.ProductListings
                .OrderByDescending(pl => pl.LastUpdated)
                .FirstOrDefault();

            if (listing == null)
            {
                return View(new PriceHistoryViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    StoreName = "Unknown",
                    History = new List<DailyPricePoint>()
                });
            }

            var history = await _context.PriceHistories
                .Where(h => h.ProductListingId == listing.Id)
                .OrderBy(h => h.RecordedAt)
                .ToListAsync();

            var dailyPrices = BuildDailyPriceHistory(
                history,
                listing.LastUpdated,
                listing.CurrentPrice);

            var viewModel = new PriceHistoryViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                StoreName = listing.Store?.Name ?? "Unknown",
                CurrentPrice = listing.CurrentPrice,
                History = dailyPrices
            };

            return View(viewModel);
        }

        private static List<DailyPricePoint> BuildDailyPriceHistory(
            List<Models.PriceHistory> history,
            DateTime listingLastUpdated,
            decimal currentPrice)
        {
            var result = new List<DailyPricePoint>();

            if (history == null || history.Count == 0)
            {
                return result;
            }

            var firstDate = history
                .Min(h => h.RecordedAt)
                .Date;

            var lastDate = DateTime.UtcNow.Date;

            if (lastDate < firstDate)
            {
                lastDate = firstDate;
            }

            var orderedHistory = history
                .OrderBy(h => h.RecordedAt)
                .ToList();

            var historyIndex = 0;
            decimal? effectivePrice = null;

            for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
            {
                while (historyIndex < orderedHistory.Count &&
                       orderedHistory[historyIndex].RecordedAt.Date <= date)
                {
                    effectivePrice = orderedHistory[historyIndex].Price;
                    historyIndex++;
                }

                if (!effectivePrice.HasValue)
                {
                    continue;
                }

                if (date == lastDate &&
                    listingLastUpdated.Date == date)
                {
                    effectivePrice = currentPrice;
                }

                result.Add(new DailyPricePoint
                {
                    Date = date,
                    Price = effectivePrice.Value
                });
            }

            return result;
        }
    }

    public class PriceHistoryViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string StoreName { get; set; } = string.Empty;

        public decimal CurrentPrice { get; set; }

        public List<DailyPricePoint> History { get; set; }
            = new List<DailyPricePoint>();
    }

    public class DailyPricePoint
    {
        public DateTime Date { get; set; }

        public decimal Price { get; set; }
    }
}