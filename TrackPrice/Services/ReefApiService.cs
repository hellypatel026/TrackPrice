using System.Text;
using System.Text.Json;
using TrackPrice.Models;

namespace TrackPrice.Services
{
    public class ReefApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ReefApiService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<ReefApiProduct>> SearchFlipkartAsync(string search)
        {
            var apiKey = _configuration["ReefApi:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("ReefAPI key is not configured.");
            }

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.reefapi.com/flipkart/v1/search");

            request.Headers.Add("x-api-key", apiKey);

            var requestBody = new
            {
                q = search,
                page = 1
            };

            var json = JsonSerializer.Serialize(requestBody);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            var responseContent =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"ReefAPI request failed: {response.StatusCode} - {responseContent}");
            }

            using var document =
                JsonDocument.Parse(responseContent);

            var results =
                document.RootElement
                    .GetProperty("data")
                    .GetProperty("results");

            var products = new List<ReefApiProduct>();

            foreach (var item in results.EnumerateArray())
            {
                var product = new ReefApiProduct
                {
                    ProductId = GetString(item, "product_id"),
                    ListingId = GetString(item, "listing_id"),
                    ItemId = GetString(item, "itm_id"),

                    Title = GetString(item, "title"),
                    Subtitle = GetString(item, "subtitle"),

                    Url = GetString(item, "url"),

                    Price = GetDecimal(item, "price"),
                    Mrp = GetNullableDecimal(item, "mrp"),

                    DiscountPercent =
                        GetDecimal(item, "discount_percent"),

                    Currency =
                        GetString(item, "currency", "INR"),

                    Rating =
                        GetDecimal(item, "rating"),

                    RatingCount =
                        GetInt(item, "rating_count"),

                    Image =
                        GetString(item, "image"),

                    Availability =
                        GetString(item, "availability"),

                    InStock =
                        GetBool(item, "in_stock"),

                    Category =
                        GetString(item, "category")
                };

                products.Add(product);
            }

            return products;
        }

        public async Task<ReefApiProduct?> GetFlipkartProductAsync(
            string url,
            string itmId)
        {
            var apiKey = _configuration["ReefApi:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("ReefAPI key is not configured.");
            }

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.reefapi.com/flipkart/v1/product");

            request.Headers.Add("x-api-key", apiKey);

            var requestBody = new
            {
                url = url,
                itm_id = itmId
            };

            var json = JsonSerializer.Serialize(requestBody);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            var responseContent =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"ReefAPI product request failed: {response.StatusCode} - {responseContent}");
            }

            using var document =
                JsonDocument.Parse(responseContent);

            var root = document.RootElement;

            if (!root.TryGetProperty("ok", out var ok) ||
                !ok.GetBoolean())
            {
                return null;
            }

            if (!root.TryGetProperty("data", out var data))
            {
                return null;
            }

            var productElement = data;

            if (data.TryGetProperty("product", out var nestedProduct))
            {
                productElement = nestedProduct;
            }

            return new ReefApiProduct
            {
                ProductId =
                    GetString(productElement, "product_id"),

                ListingId =
                    GetString(productElement, "listing_id"),

                ItemId =
                    GetString(productElement, "itm_id"),

                Title =
                    GetString(productElement, "title"),

                Subtitle =
                    GetString(productElement, "subtitle"),

                Url =
                    GetString(productElement, "url"),

                Price =
                    GetDecimal(productElement, "price"),

                Mrp =
                    GetNullableDecimal(productElement, "mrp"),

                DiscountPercent =
                    GetDecimal(
                        productElement,
                        "discount_percent"),

                Currency =
                    GetString(
                        productElement,
                        "currency",
                        "INR"),

                Rating =
                    GetDecimal(productElement, "rating"),

                RatingCount =
                    GetInt(productElement, "rating_count"),

                Image =
                    GetString(productElement, "image"),

                Availability =
                    GetString(productElement, "availability"),

                InStock =
                    GetBool(productElement, "in_stock"),

                Category =
                    GetString(productElement, "category")
            };
        }

        private static string GetString(
            JsonElement element,
            string property,
            string defaultValue = "")
        {
            if (element.TryGetProperty(property, out var value) &&
                value.ValueKind != JsonValueKind.Null)
            {
                return value.ToString();
            }

            return defaultValue;
        }

        private static decimal GetDecimal(
    JsonElement element,
    string property)
        {
            if (!element.TryGetProperty(property, out var value))
            {
                return 0;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return 0;
            }

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetDecimal(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.GetString(), out var stringResult))
            {
                return stringResult;
            }

            return 0;
        }

        private static decimal? GetNullableDecimal(
            JsonElement element,
            string property)
        {
            if (element.TryGetProperty(property, out var value) &&
                value.ValueKind != JsonValueKind.Null &&
                value.TryGetDecimal(out var result))
            {
                return result;
            }

            return null;
        }

        private static int GetInt(
            JsonElement element,
            string property)
        {
            if (element.TryGetProperty(property, out var value) &&
                value.TryGetInt32(out var result))
            {
                return result;
            }

            return 0;
        }

        private static bool GetBool(
            JsonElement element,
            string property)
        {
            if (element.TryGetProperty(property, out var value) &&
                value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            return false;
        }
    }
}