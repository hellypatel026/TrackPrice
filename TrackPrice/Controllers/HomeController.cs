using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TrackPrice.Models;
using TrackPrice.Services;

namespace TrackPrice.Controllers
{
    public class HomeController : Controller
    {
        private readonly ReefApiService _reefApiService;

        private readonly AmazonApiService _amazonApiService;

        public HomeController(
            ReefApiService reefApiService,
            AmazonApiService amazonApiService)
        {
            _reefApiService = reefApiService;
            _amazonApiService = amazonApiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TestReefApi(
            string query = "laptop")
        {
            var products =
                await _reefApiService.SearchFlipkartAsync(query);

            return Json(products);
        }
        [HttpGet]
        public async Task<IActionResult> TestAmazonApi(string query = "laptop")
        {
            var products =
                await _amazonApiService.SearchAmazonAsync(query);

            return Json(products);
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}