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
    private readonly ILogger<CurrencyController> _logger;

    // Cache snapshot is replaced atomically; the semaphore prevents concurrent
    // cold-cache requests from all hitting cbu.uz at once.
    private static IReadOnlyDictionary<string, decimal> _ratesCache = new Dictionary<string, decimal>();
    private static DateTime _lastUpdated = DateTime.MinValue;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    public CurrencyController(IHttpClientFactory httpClientFactory, ILogger<CurrencyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("rates")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRates()
    {
        if (IsCacheFresh())
            return Ok(new { rates = _ratesCache, lastUpdated = _lastUpdated, source = "CBU" });

        await _refreshLock.WaitAsync();
        try
        {
            if (IsCacheFresh())
                return Ok(new { rates = _ratesCache, lastUpdated = _lastUpdated, source = "CBU" });

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
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

            return Ok(new { rates, lastUpdated = _lastUpdated, source = "CBU" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh currency rates from CBU");
            if (_ratesCache.Count > 0)
                return Ok(new { rates = _ratesCache, lastUpdated = _lastUpdated, source = "CBU (cached)" });

            return StatusCode(503, new { message = "Currency service unavailable" });
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static bool IsCacheFresh()
        => DateTime.UtcNow - _lastUpdated < TimeSpan.FromHours(6) && _ratesCache.Count > 0;
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
