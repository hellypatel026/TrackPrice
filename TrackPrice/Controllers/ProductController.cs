using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackPrice.Data;
using TrackPrice.Models;
using TrackPrice.Services;

namespace TrackPrice.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ReefApiService _reefApiService;
        private readonly AmazonApiService _amazonApiService;
        private readonly ApplicationDbContext _context;

        public ProductsController(
            ReefApiService reefApiService,
            AmazonApiService amazonApiService,
            ApplicationDbContext context)
        {
            _reefApiService = reefApiService;
            _amazonApiService = amazonApiService;
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return View(new List<ReefApiProduct>());
            }

            search = search.Trim();

            var products =
                await _reefApiService.SearchFlipkartAsync(search);

            ViewBag.Search = search;

            return View(products);
        }

        public async Task<IActionResult> Details(
            string url,
            string itmId)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                string.IsNullOrWhiteSpace(itmId))
            {
                return BadRequest();
            }

            // Get selected Flipkart product
            var flipkartProduct =
                await _reefApiService.GetFlipkartProductAsync(
                    url,
                    itmId);

            if (flipkartProduct == null)
            {
                return NotFound();
            }

            // Make sure this API product exists in the local database.
            // Price Alerts and Watchlist use the local Product.Id.
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.ExternalProductId == flipkartProduct.ProductId &&
                    p.Source == "Flipkart");

            if (product == null)
            {
                product = new Product
                {
                    Name = flipkartProduct.Title,
                    Description = flipkartProduct.Subtitle,
                    Category = flipkartProduct.Category,
                    ImageUrl = flipkartProduct.Image,
                    ExternalProductId = flipkartProduct.ProductId,
                    Source = "Flipkart",
                    ProductUrl = flipkartProduct.Url,
                    ItemId = flipkartProduct.ItemId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }

            // Make the local Product.Id available to the Details view.
            ViewBag.ProductId = product.Id;

            // Search Amazon using the Flipkart product title
            var amazonProducts =
                await _amazonApiService.SearchAmazonAsync(
                    flipkartProduct.Title);

            // Keep only relevant Amazon products
            var matchingAmazonProducts =
                amazonProducts
                    .Select(product => new
                    {
                        Product = product,
                        Score = CalculateMatchScore(
                            flipkartProduct.Title,
                            product.Title)
                    })
                    .Where(x => x.Score >= 0.35)
                    .OrderByDescending(x => x.Score)
                    .Take(3)
                    .Select(x => x.Product)
                    .ToList();

            // Create comparison model
            var comparisonModel =
                new ProductComparisonViewModel
                {
                    FlipkartProduct = flipkartProduct,
                    AmazonProducts = matchingAmazonProducts
                };

            return View(comparisonModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWatchlist(
    string url,
    string itmId)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                string.IsNullOrWhiteSpace(itmId))
            {
                return BadRequest();
            }

            // Get the selected Flipkart product
            var flipkartProduct =
                await _reefApiService.GetFlipkartProductAsync(
                    url,
                    itmId);

            if (flipkartProduct == null)
            {
                return NotFound();
            }

            // Check whether this API product already exists
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.ExternalProductId == flipkartProduct.ProductId &&
                        p.Source == "Flipkart");

            // If it does not exist, create a local Product
            if (product == null)
            {
                product = new Product
                {
                    Name = flipkartProduct.Title,
                    Description = flipkartProduct.Subtitle,
                    Category = flipkartProduct.Category,
                    ImageUrl = flipkartProduct.Image,
                    ExternalProductId = flipkartProduct.ProductId,
                    Source = "Flipkart",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);

                await _context.SaveChangesAsync();
            }

            // Get logged-in user's ID
            var userId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Check whether it is already in the watchlist
            var alreadyExists =
                await _context.Watchlists
                    .AnyAsync(w =>
                        w.UserId == userId &&
                        w.ProductId == product.Id);

            if (!alreadyExists)
            {
                var watchlistItem = new Watchlist
                {
                    UserId = userId,
                    ProductId = product.Id,
                    AddedAt = DateTime.UtcNow
                };

                _context.Watchlists.Add(watchlistItem);

                await _context.SaveChangesAsync();
            }

            // Return to the same product details page
            return RedirectToAction(
                "Details",
                new
                {
                    url = flipkartProduct.Url,
                    itmId = flipkartProduct.ItemId
                });
        }

        private static double CalculateMatchScore(
            string flipkartTitle,
            string amazonTitle)
        {
            var flipkartWords =
                GetImportantWords(flipkartTitle);

            var amazonWords =
                GetImportantWords(amazonTitle);

            if (flipkartWords.Count == 0 ||
                amazonWords.Count == 0)
            {
                return 0;
            }

            var matchingWords =
                flipkartWords
                    .Intersect(amazonWords)
                    .Count();

            return (double)matchingWords /
                   flipkartWords.Count;
        }

        private static HashSet<string> GetImportantWords(
            string title)
        {
            var stopWords = new HashSet<string>
            {
                "the",
                "and",
                "with",
                "for",
                "from",
                "inch",
                "in",
                "gb",
                "kg",
                "cm",
                "new",
                "latest",
                "laptop",
                "computer",
                "notebook",
                "available",
                "storage",
                "display",
                "screen",
                "processor",
                "windows",
                "home",
                "office"
            };

            var words =
                System.Text.RegularExpressions.Regex.Split(
                    title.ToLowerInvariant(),
                    @"[^a-z0-9]+")
                .Where(word =>
                    word.Length >= 2 &&
                    !stopWords.Contains(word))
                .ToHashSet();

            return words;
        }
    }
}