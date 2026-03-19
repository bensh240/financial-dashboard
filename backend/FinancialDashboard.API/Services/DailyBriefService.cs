using FinancialDashboard.API.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialDashboard.API.Services;

/// <summary>
/// Hangfire recurring job – runs at 8 AM (configurable via Settings).
/// Fetches fresh prices, generates an AI-style summary, and emails it.
///
/// AI Enhancement: replace GenerateSummary() with a call to the Claude API
/// (https://api.anthropic.com/v1/messages) using the stock data as context.
/// </summary>
public class DailyBriefService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DailyBriefService> _logger;

    public DailyBriefService(IServiceProvider services, ILogger<DailyBriefService> logger)
    {
        _services = services;
        _logger   = logger;
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

        var html = BuildHtml(quotes, recentAl, DateTime.UtcNow);
        await emailService.SendAsync(settings.Email, $"📊 Daily Brief – {DateTime.UtcNow:MMM d, yyyy}", html);
    }

    private static string BuildHtml(
        List<(string Symbol, Models.FinnhubQuote? Quote)> quotes,
        List<Models.AlertHistory> alerts,
        DateTime now)
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

        var allRows  = string.Join("", quotes.Select(QuoteRow));
        var gainerRows = string.Join("", gainers.Select(QuoteRow));
        var loserRows  = string.Join("", losers.Select(QuoteRow));

        var alertRows = alerts.Count == 0
            ? "<tr><td colspan='4'>No alerts in the last 24 hours</td></tr>"
            : string.Join("", alerts.Select(a =>
                $"<tr><td>{a.Symbol}</td><td>{a.AlertType}</td>" +
                $"<td style='color:{(a.PercentChange >= 0 ? "green" : "red")}'>{a.PercentChange:+0.00;-0.00}%</td>" +
                $"<td>{a.TriggeredAt:HH:mm UTC}</td></tr>"));

        // --- AI Enhancement placeholder ---
        // To integrate Claude AI for a narrative summary, call:
        // POST https://api.anthropic.com/v1/messages with the stock data JSON
        // and prompt: "Write a concise market brief for these stocks: {data}"
        var aiSummary = GenerateRuleSummary(quotes);

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
