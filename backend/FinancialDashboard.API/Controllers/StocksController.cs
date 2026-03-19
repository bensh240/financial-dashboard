using FinancialDashboard.API.Models;
using FinancialDashboard.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialDashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StocksController : ControllerBase
{
    private readonly IFinnhubService _finnhub;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<StocksController> _logger;

    public StocksController(IFinnhubService finnhub, IHttpClientFactory httpFactory, ILogger<StocksController> logger)
    {
        _finnhub     = finnhub;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    /// <summary>Get the latest quote for any symbol.</summary>
    [HttpGet("{symbol}/quote")]
    public async Task<IActionResult> GetQuote(string symbol)
    {
        var quote = await _finnhub.GetQuoteAsync(symbol.ToUpperInvariant());
        if (quote == null) return NotFound(new { error = $"No data for '{symbol}'." });
        return Ok(quote);
    }

    /// <summary>Get company profile.</summary>
    [HttpGet("{symbol}/profile")]
    public async Task<IActionResult> GetProfile(string symbol)
    {
        var profile = await _finnhub.GetCompanyProfileAsync(symbol.ToUpperInvariant());
        if (profile == null) return NotFound(new { error = $"No profile for '{symbol}'." });
        return Ok(profile);
    }

    /// <summary>Search for stock symbols by name or ticker (all types including international).</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<object>());

        var result = await _finnhub.SearchSymbolsAsync(q);
        if (result?.Result == null) return Ok(new List<object>());

        var filtered = result.Result
            .Take(8)
            .Select(r => new { symbol = r.Symbol, description = r.Description })
            .ToList();

        return Ok(filtered);
    }

    /// <summary>
    /// Historical OHLCV chart via Yahoo Finance (free, no key required).
    /// days: 7 | 30 | 90 | 365
    /// </summary>
    [HttpGet("{symbol}/candles")]
    public async Task<IActionResult> GetCandles(string symbol, [FromQuery] int days = 30)
    {
        // Map days → Yahoo Finance range & interval
        var (range, interval) = days switch
        {
            <= 7   => ("5d",  "1d"),
            <= 30  => ("1mo", "1d"),
            <= 90  => ("3mo", "1d"),
            _      => ("1y",  "1wk"),
        };

        try
        {
            var client = _httpFactory.CreateClient("Yahoo");
            var url    = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol.ToUpperInvariant())}?interval={interval}&range={range}";

            var response = await client.GetFromJsonAsync<YahooChartResponse>(url);
            var result   = response?.Chart?.Result?.FirstOrDefault();

            if (result?.Timestamps == null || result.Indicators?.Quote == null)
                return NotFound(new { error = $"No chart data for '{symbol}'." });

            var quoteData = result.Indicators.Quote[0];
            var opens     = quoteData.Open;
            var highs     = quoteData.High;
            var lows      = quoteData.Low;
            var closes    = quoteData.Close;
            var volumes   = quoteData.Volume;

            if (closes == null)
                return NotFound(new { error = $"No close prices for '{symbol}'." });

            var points = result.Timestamps
                .Select((t, i) => new
                {
                    t,
                    o = opens  != null && i < opens.Count  ? opens[i]  : null,
                    h = highs  != null && i < highs.Count  ? highs[i]  : null,
                    l = lows   != null && i < lows.Count   ? lows[i]   : null,
                    c = i < closes.Count                   ? closes[i] : null,
                    v = volumes != null && i < volumes.Count ? volumes[i] : null
                })
                .Where(x => x.c.HasValue && x.o.HasValue && x.h.HasValue && x.l.HasValue)
                .Select(x => new
                {
                    timestamp = DateTimeOffset.FromUnixTimeSeconds(x.t).DateTime,
                    open      = x.o!.Value,
                    high      = x.h!.Value,
                    low       = x.l!.Value,
                    close     = x.c!.Value,
                    volume    = x.v ?? 0L
                })
                .ToList();

            return Ok(points);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Yahoo Finance chart fetch failed for {Symbol}", symbol);
            return NotFound(new { error = $"Could not load chart data for '{symbol}'." });
        }
    }
}
