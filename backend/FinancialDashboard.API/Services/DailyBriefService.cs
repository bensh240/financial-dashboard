using System.Text;
using System.Text.Json;
using FinancialDashboard.API.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialDashboard.API.Services;

/// <summary>
/// Hangfire recurring job – runs at 8 AM (configurable via Settings).
/// Fetches fresh prices, generates an AI summary via Claude, and emails it.
/// </summary>
public class DailyBriefService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DailyBriefService> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public DailyBriefService(
        IServiceProvider services,
        ILogger<DailyBriefService> logger,
        IHttpClientFactory httpFactory,
        IConfiguration config)
    {
        _services    = services;
        _logger      = logger;
        _httpFactory = httpFactory;
        _config      = config;
    }

    public async Task SendDailyBriefAsync()
    {
        _logger.LogInformation("DailyBriefService: generating morning brief");

        using var scope   = _services.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var finnhub       = scope.ServiceProvider.GetRequiredService<IFinnhubService>();
        var emailService  = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var settings  = await db.UserSettings.FirstAsync();
        if (!settings.DailyBriefEnabled || string.IsNullOrWhiteSpace(settings.Email)) return;

        var watchlist = await db.WatchlistItems.ToListAsync();
        if (watchlist.Count == 0) return;

        var quotes = await finnhub.GetQuotesBatchAsync(watchlist.Select(w => w.Symbol));

        // Recent alerts (last 24 h)
        var since    = DateTime.UtcNow.AddHours(-24);
        var recentAl = await db.AlertHistories
            .Where(h => h.TriggeredAt >= since)
            .OrderByDescending(h => h.TriggeredAt)
            .Take(10)
            .ToListAsync();

        // Get Claude API key from UserSettings first, fall back to appsettings
        var claudeKey = !string.IsNullOrWhiteSpace(settings.ClaudeApiKey)
            ? settings.ClaudeApiKey
            : (_config["Anthropic:ApiKey"] ?? "");

        var aiSummary = await GenerateClaudeSummary(quotes, claudeKey);
        var html = BuildHtml(quotes, recentAl, DateTime.UtcNow, aiSummary);
        await emailService.SendAsync(settings.Email, $"📊 Daily Brief – {DateTime.UtcNow:MMM d, yyyy}", html);
    }

    private async Task<string> GenerateClaudeSummary(
        List<(string Symbol, Models.FinnhubQuote? Quote)> quotes,
        string claudeApiKey)
    {
        if (string.IsNullOrWhiteSpace(claudeApiKey))
            return GenerateRuleSummary(quotes);

        try
        {
            var stockData = quotes
                .Where(q => q.Quote != null)
                .Select(q => new
                {
                    symbol        = q.Symbol,
                    price         = q.Quote!.CurrentPrice,
                    percentChange = q.Quote.PercentChange
                });

            var stockJson = JsonSerializer.Serialize(stockData);

            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", claudeApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var requestBody = new
            {
                model      = "claude-sonnet-4-6",
                max_tokens = 300,
                messages   = new[]
                {
                    new
                    {
                        role    = "user",
                        content = $"Write a concise 2-sentence market brief for a financial dashboard based on these stocks: {stockJson}. Focus on overall market sentiment and the biggest mover."
                    }
                }
            };

            var json     = JsonSerializer.Serialize(requestBody);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Claude API returned {Status}, falling back to rule-based summary", response.StatusCode);
                return GenerateRuleSummary(quotes);
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc    = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            return text ?? GenerateRuleSummary(quotes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude API call failed, falling back to rule-based summary");
            return GenerateRuleSummary(quotes);
        }
    }

    private static string BuildHtml(
        List<(string Symbol, Models.FinnhubQuote? Quote)> quotes,
        List<Models.AlertHistory> alerts,
        DateTime now,
        string aiSummary)
    {
        var gainers = quotes
            .Where(q => q.Quote != null && q.Quote.PercentChange > 0)
            .OrderByDescending(q => q.Quote!.PercentChange)
            .Take(3)
            .ToList();

        var losers = quotes
            .Where(q => q.Quote != null && q.Quote.PercentChange < 0)
            .OrderBy(q => q.Quote!.PercentChange)
            .Take(3)
            .ToList();

        string QuoteRow((string Symbol, Models.FinnhubQuote? Quote) q) => q.Quote == null
            ? $"<tr><td>{q.Symbol}</td><td colspan='3'>N/A</td></tr>"
            : $"<tr><td><b>{q.Symbol}</b></td><td>${q.Quote.CurrentPrice:F2}</td>" +
              $"<td style='color:{(q.Quote.PercentChange >= 0 ? "green" : "red")}'>" +
              $"{q.Quote.PercentChange:+0.00;-0.00}%</td>" +
              $"<td>${q.Quote.PreviousClose:F2}</td></tr>";

        var allRows    = string.Join("", quotes.Select(QuoteRow));
        var gainerRows = string.Join("", gainers.Select(QuoteRow));
        var loserRows  = string.Join("", losers.Select(QuoteRow));

        var alertRows = alerts.Count == 0
            ? "<tr><td colspan='4'>No alerts in the last 24 hours</td></tr>"
            : string.Join("", alerts.Select(a =>
                $"<tr><td>{a.Symbol}</td><td>{a.AlertType}</td>" +
                $"<td style='color:{(a.PercentChange >= 0 ? "green" : "red")}'>{a.PercentChange:+0.00;-0.00}%</td>" +
                $"<td>{a.TriggeredAt:HH:mm UTC}</td></tr>"));

        return $"""
            <div style="font-family:Arial,sans-serif;max-width:700px">
              <h1 style="color:#1a365d">📊 Daily Market Brief – {now:MMMM d, yyyy}</h1>
              <p><em>{aiSummary}</em></p>

              <h2>📈 Top Gainers</h2>
              <table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse;width:100%">
                <thead style="background:#e6f4ea"><tr><th>Symbol</th><th>Price</th><th>Change</th><th>Prev. Close</th></tr></thead>
                <tbody>{gainerRows}</tbody>
              </table>

              <h2>📉 Top Losers</h2>
              <table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse;width:100%">
                <thead style="background:#fce8e6"><tr><th>Symbol</th><th>Price</th><th>Change</th><th>Prev. Close</th></tr></thead>
                <tbody>{loserRows}</tbody>
              </table>

              <h2>📋 Full Watchlist</h2>
              <table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse;width:100%">
                <thead style="background:#e8f0fe"><tr><th>Symbol</th><th>Price</th><th>Change</th><th>Prev. Close</th></tr></thead>
                <tbody>{allRows}</tbody>
              </table>

              <h2>⚡ Recent Alerts (24 h)</h2>
              <table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse;width:100%">
                <thead style="background:#fff3cd"><tr><th>Symbol</th><th>Type</th><th>Change</th><th>Time</th></tr></thead>
                <tbody>{alertRows}</tbody>
              </table>

              <p style="color:#666;font-size:12px">Generated by Financial Dashboard at {now:yyyy-MM-dd HH:mm} UTC</p>
            </div>
            """;
    }

    private static string GenerateRuleSummary(List<(string Symbol, Models.FinnhubQuote? Quote)> quotes)
    {
        var valid   = quotes.Where(q => q.Quote != null).ToList();
        if (valid.Count == 0) return "No price data available today.";

        var avgChange = valid.Average(q => (double)q.Quote!.PercentChange);
        var mood      = avgChange > 0.5 ? "bullish" : avgChange < -0.5 ? "bearish" : "mixed";
        var leader    = valid.OrderByDescending(q => Math.Abs((double)q.Quote!.PercentChange)).First();

        return $"Your watchlist is showing a {mood} tone today with an average move of {avgChange:+0.00;-0.00}%. " +
               $"Biggest mover: {leader.Symbol} at {leader.Quote!.PercentChange:+0.00;-0.00}%.";
    }
}
