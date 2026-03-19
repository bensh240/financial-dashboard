using FinancialDashboard.API.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialDashboard.API.Services;

/// <summary>
/// Hangfire recurring job – runs every Sunday at 9 AM.
/// Compiles a weekly performance report from alert history and current prices.
/// </summary>
public class WeeklyReportService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WeeklyReportService> _logger;

    public WeeklyReportService(IServiceProvider services, ILogger<WeeklyReportService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    public async Task SendWeeklyReportAsync()
    {
        _logger.LogInformation("WeeklyReportService: generating weekly report");

        using var scope  = _services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var finnhub      = scope.ServiceProvider.GetRequiredService<IFinnhubService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var settings = await db.UserSettings.FirstAsync();
        if (!settings.WeeklyReportEnabled || string.IsNullOrWhiteSpace(settings.Email)) return;

        var watchlist = await db.WatchlistItems.ToListAsync();
        if (watchlist.Count == 0) return;

        var weekStart = DateTime.UtcNow.AddDays(-7);
        var weeklyAlerts = await db.AlertHistories
            .Where(h => h.TriggeredAt >= weekStart)
            .OrderByDescending(h => h.TriggeredAt)
            .ToListAsync();

        var quotes = await finnhub.GetQuotesBatchAsync(watchlist.Select(w => w.Symbol));

        var html = BuildWeeklyHtml(quotes, weeklyAlerts, watchlist, weekStart, DateTime.UtcNow);
        await emailService.SendAsync(settings.Email, $"📊 Weekly Performance Report – {DateTime.UtcNow:MMM d, yyyy}", html);
    }

    private static string BuildWeeklyHtml(
        List<(string Symbol, Models.FinnhubQuote? Quote)> quotes,
        List<Models.AlertHistory> alerts,
        List<Models.WatchlistItem> watchlist,
        DateTime weekStart,
        DateTime weekEnd)
    {
        // Alert breakdown by type
        var spikeCount      = alerts.Count(a => a.AlertType == "SPIKE");
        var dropCount       = alerts.Count(a => a.AlertType == "DROP");
        var volatilityCount = alerts.Count(a => a.AlertType == "VOLATILITY");

        // Per-symbol summary
        var symbolStats = watchlist.Select(w =>
        {
            var q = quotes.FirstOrDefault(x => x.Symbol == w.Symbol).Quote;
            var symbolAlerts = alerts.Where(a => a.Symbol == w.Symbol).ToList();
            return new
            {
                w.Symbol,
                w.CompanyName,
                CurrentPrice = q?.CurrentPrice,
                WeekChange   = q?.PercentChange,
                AlertCount   = symbolAlerts.Count,
                MaxSwing     = symbolAlerts.Count > 0
                    ? symbolAlerts.Max(a => Math.Abs(a.PercentChange))
                    : (decimal?)null
            };
        }).ToList();

        var symbolRows = string.Join("", symbolStats.Select(s =>
        {
            var color = s.WeekChange >= 0 ? "green" : "red";
            return $"<tr>" +
                   $"<td><b>{s.Symbol}</b></td>" +
                   $"<td>{s.CompanyName ?? "—"}</td>" +
                   $"<td>{(s.CurrentPrice.HasValue ? $"${s.CurrentPrice:F2}" : "N/A")}</td>" +
                   $"<td style='color:{color}'>{(s.WeekChange.HasValue ? $"{s.WeekChange:+0.00;-0.00}%" : "N/A")}</td>" +
                   $"<td>{s.AlertCount}</td>" +
                   $"<td>{(s.MaxSwing.HasValue ? $"{s.MaxSwing:F2}%" : "—")}</td>" +
                   "</tr>";
        }));

        var recentAlertRows = alerts.Count == 0
            ? "<tr><td colspan='5'>No alerts this week</td></tr>"
            : string.Join("", alerts.Take(20).Select(a =>
                $"<tr><td>{a.Symbol}</td><td>{a.AlertType}</td>" +
                $"<td style='color:{(a.PercentChange >= 0 ? "green" : "red")}'>{a.PercentChange:+0.00;-0.00}%</td>" +
                $"<td>${a.PriceAtTrigger:F2}</td>" +
                $"<td>{a.TriggeredAt:MMM d HH:mm UTC}</td></tr>"));

        return $"""
            <div style="font-family:Arial,sans-serif;max-width:750px">
              <h1 style="color:#1a365d">📊 Weekly Performance Report</h1>
              <p style="color:#555">{weekStart:MMM d} – {weekEnd:MMM d, yyyy}</p>

              <h2>📊 Weekly Alert Summary</h2>
              <table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse">
                <tr><td><b>Total Alerts</b></td><td>{alerts.Count}</td></tr>
                <tr><td>🟢 Spikes</td><td>{spikeCount}</td></tr>
                <tr><td>🔴 Drops</td><td>{dropCount}</td></tr>
                <tr><td>⚡ Volatility Events</td><td>{volatilityCount}</td></tr>
              </table>

              <h2>📋 Watchlist Performance</h2>
              <table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse;width:100%">
                <thead style="background:#e8f0fe">
                  <tr><th>Symbol</th><th>Company</th><th>Current</th><th>Day Change</th><th>Alerts</th><th>Max Swing</th></tr>
                </thead>
                <tbody>{symbolRows}</tbody>
              </table>

              <h2>⚡ Alert Log (last 20)</h2>
              <table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse;width:100%">
                <thead style="background:#fff3cd">
                  <tr><th>Symbol</th><th>Type</th><th>Change</th><th>Price</th><th>Time</th></tr>
                </thead>
                <tbody>{recentAlertRows}</tbody>
              </table>

              <p style="color:#666;font-size:12px">Generated by Financial Dashboard at {weekEnd:yyyy-MM-dd HH:mm} UTC</p>
            </div>
            """;
    }
}
