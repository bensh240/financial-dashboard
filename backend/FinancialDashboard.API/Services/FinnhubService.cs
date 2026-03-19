using FinancialDashboard.API.Models;

namespace FinancialDashboard.API.Services;

public class FinnhubService : IFinnhubService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<FinnhubService> _logger;

    public FinnhubService(HttpClient http, IConfiguration cfg, ILogger<FinnhubService> logger)
    {
        _http   = http;
        _logger = logger;
        _apiKey = cfg["Finnhub:ApiKey"] ?? "";
        if (string.IsNullOrWhiteSpace(_apiKey))
            _logger.LogWarning("Finnhub:ApiKey is not configured – stock price calls will fail. Set it in .env or appsettings.");
        _http.BaseAddress = new Uri("https://finnhub.io/api/v1/");
    }

    public async Task<FinnhubQuote?> GetQuoteAsync(string symbol)
    {
        try
        {
            var url = $"quote?symbol={Uri.EscapeDataString(symbol)}&token={_apiKey}";
            var quote = await _http.GetFromJsonAsync<FinnhubQuote>(url);
            // Finnhub returns all zeros for unknown symbols
            return quote?.CurrentPrice == 0 ? null : quote;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch quote for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<FinnhubProfile?> GetCompanyProfileAsync(string symbol)
    {
        try
        {
            var url = $"stock/profile2?symbol={Uri.EscapeDataString(symbol)}&token={_apiKey}";
            return await _http.GetFromJsonAsync<FinnhubProfile>(url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch profile for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<FinnhubCandle?> GetCandlesAsync(string symbol, string resolution, long from, long to)
    {
        try
        {
            var url = $"stock/candle?symbol={Uri.EscapeDataString(symbol)}&resolution={resolution}&from={from}&to={to}&token={_apiKey}";
            var candle = await _http.GetFromJsonAsync<FinnhubCandle>(url);
            return candle?.Status == "ok" ? candle : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch candles for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<FinnhubSearchResponse?> SearchSymbolsAsync(string query)
    {
        try
        {
            var url = $"search?q={Uri.EscapeDataString(query)}&token={_apiKey}";
            return await _http.GetFromJsonAsync<FinnhubSearchResponse>(url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search for '{Query}'", query);
            return null;
        }
    }

    public async Task<List<FinnhubNewsItem>> GetMarketNewsAsync(string category = "general")
    {
        try
        {
            var url = $"news?category={category}&token={_apiKey}";
            var items = await _http.GetFromJsonAsync<List<FinnhubNewsItem>>(url);
            return items ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch market news");
            return [];
        }
    }

    public async Task<List<FinnhubNewsItem>> GetCompanyNewsAsync(string symbol)
    {
        try
        {
            var from = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
            var to   = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var url  = $"company-news?symbol={Uri.EscapeDataString(symbol)}&from={from}&to={to}&token={_apiKey}";
            var items = await _http.GetFromJsonAsync<List<FinnhubNewsItem>>(url);
            return items?.Take(20).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch company news for {Symbol}", symbol);
            return [];
        }
    }

    public async Task<List<(string Symbol, FinnhubQuote? Quote)>> GetQuotesBatchAsync(IEnumerable<string> symbols)
    {
        // Finnhub free tier: no batch endpoint; fetch sequentially with a small delay to respect rate limits
        var results = new List<(string, FinnhubQuote?)>();
        foreach (var sym in symbols)
        {
            var quote = await GetQuoteAsync(sym);
            results.Add((sym, quote));
            await Task.Delay(200); // 5 req/sec free tier
        }
        return results;
    }
}
