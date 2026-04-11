using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/currency")]
public class CurrencyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static Dictionary<string, decimal> _ratesCache = new();
    private static DateTime _lastUpdated = DateTime.MinValue;

    public CurrencyController(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    [HttpGet("rates")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRates()
    {
        if (DateTime.UtcNow - _lastUpdated < TimeSpan.FromHours(6)
            && _ratesCache.Count > 0)
        {
            return Ok(new
            {
                rates = _ratesCache,
                lastUpdated = _lastUpdated,
                source = "CBU"
            });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var currencies = new[] { "USD", "EUR", "RUB", "CNY", "GBP" };
            var rates = new Dictionary<string, decimal>();

            foreach (var currency in currencies)
            {
                var url = $"https://cbu.uz/uz/arkhiv-kursov-valyut/json/{currency}/";
                var response = await client.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<List<CbuRateDto>>(response);
                if (data != null && data.Count > 0)
                {
                    rates[currency] = decimal.Parse(
                        data[0].Rate,
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                }
            }

            _ratesCache = rates;
            _lastUpdated = DateTime.UtcNow;

            return Ok(new
            {
                rates,
                lastUpdated = _lastUpdated,
                source = "CBU"
            });
        }
        catch
        {
            if (_ratesCache.Count > 0)
                return Ok(new { rates = _ratesCache, lastUpdated = _lastUpdated, source = "CBU (cached)" });

            return StatusCode(503, new { message = "Currency service unavailable" });
        }
    }
}

public class CbuRateDto
{
    [JsonPropertyName("Ccy")]
    public string Currency { get; set; } = null!;

    [JsonPropertyName("Rate")]
    public string Rate { get; set; } = null!;

    [JsonPropertyName("Date")]
    public string Date { get; set; } = null!;
}
