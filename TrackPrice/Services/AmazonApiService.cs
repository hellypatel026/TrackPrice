using System.Text;
using System.Text.Json;
using TrackPrice.Models;

namespace TrackPrice.Services
{
    public class AmazonApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AmazonApiService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<AmazonApiProduct>> SearchAmazonAsync(
            string search)
        {
            var apiKey = _configuration["ReefApi:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "ReefAPI key is not configured.");
            }

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.reefapi.com/amazon/v1/search");

            request.Headers.Add("x-api-key", apiKey);

            var requestBody = new
            {
                query = search,
                marketplace = "in",
                page = 1,
                max_results = 10
            };

            var json = JsonSerializer.Serialize(requestBody);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response =
                await _httpClient.SendAsync(request);

            var responseContent =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Amazon API request failed: " +
                    $"{response.StatusCode} - {responseContent}");
            }

            using var document =
                JsonDocument.Parse(responseContent);

            var root = document.RootElement;

            if (!root.TryGetProperty("ok", out var ok) ||
                ok.ValueKind != JsonValueKind.True)
            {
                throw new Exception(
                    $"Amazon API returned an unsuccessful response: " +
                    $"{responseContent}");
            }

            if (!root.TryGetProperty("data", out var data))
            {
                return new List<AmazonApiProduct>();
            }

            if (!data.TryGetProperty("results", out var results))
            {
                return new List<AmazonApiProduct>();
            }

            var products =
                new List<AmazonApiProduct>();

            foreach (var item in results.EnumerateArray())
            {
                var asin =
                    GetString(item, "asin");

                var amazonUrl =
                    GetString(item, "url");

                // ReefAPI may return an empty URL.
                // Build the Amazon India product URL using ASIN.
                if (string.IsNullOrWhiteSpace(amazonUrl) &&
                    !string.IsNullOrWhiteSpace(asin))
                {
                    amazonUrl =
                        $"https://www.amazon.in/dp/{asin}";
                }

                var product = new AmazonApiProduct
                {
                    Asin =
                        asin,

                    Title =
                        GetString(item, "title"),

                    Price =
                        GetDecimal(item, "price"),

                    ListPrice =
                        GetNullableDecimal(item, "list_price"),

                    Currency =
                        GetString(item, "currency", "₹"),

                    Rating =
                        GetDecimal(item, "rating"),

                    RatingCount =
                        GetInt(item, "rating_count"),

                    Image =
                        GetString(item, "image"),

                    Url =
                        amazonUrl,

                    Availability =
                        GetString(item, "availability"),

                    InStock =
                        GetBool(item, "in_stock")
                };

                products.Add(product);
            }

            return products;
        }

        private static string GetString(
            JsonElement element,
            string property,
            string defaultValue = "")
        {
            if (!element.TryGetProperty(
                    property,
                    out var value))
            {
                return defaultValue;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return defaultValue;
            }

            return value.ToString();
        }

        private static decimal GetDecimal(
            JsonElement element,
            string property)
        {
            if (!element.TryGetProperty(
                    property,
                    out var value))
            {
                return 0;
            }

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetDecimal(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(
                    value.GetString(),
                    out var stringResult))
            {
                return stringResult;
            }

            return 0;
        }

        private static decimal? GetNullableDecimal(
            JsonElement element,
            string property)
        {
            if (!element.TryGetProperty(
                    property,
                    out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetDecimal(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(
                    value.GetString(),
                    out var stringResult))
            {
                return stringResult;
            }

            return null;
        }

        private static int GetInt(
            JsonElement element,
            string property)
        {
            if (!element.TryGetProperty(
                    property,
                    out var value))
            {
                return 0;
            }

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(
                    value.GetString(),
                    out var stringResult))
            {
                return stringResult;
            }

            return 0;
        }

        private static bool GetBool(
            JsonElement element,
            string property)
        {
            if (!element.TryGetProperty(
                    property,
                    out var value))
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.String &&
                bool.TryParse(
                    value.GetString(),
                    out var result))
            {
                return result;
            }

            return false;
        }
    }
}