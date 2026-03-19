using FinancialDashboard.API.Data;
using FinancialDashboard.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialDashboard.API.Services;

/// <summary>
/// Background service that polls Finnhub every 5 minutes, updates cached prices,
/// evaluates threshold-based alerts, and delegates volatility detection.
/// </summary>
public class PriceMonitorService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _services;
    private readonly ILogger<PriceMonitorService> _logger;

    public PriceMonitorService(IServiceProvider services, ILogger<PriceMonitorService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PriceMonitorService started – polling every {Interval}", PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during price poll cycle");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        using var scope     = _services.CreateScope();
        var db              = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var finnhub         = scope.ServiceProvider.GetRequiredService<IFinnhubService>();
        var email           = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var volatility      = scope.ServiceProvider.GetRequiredService<VolatilityDetectorService>();

        var watchlist = await db.WatchlistItems.ToListAsync(ct);
        if (watchlist.Count == 0) return;

        var alerts   = await db.PriceAlerts.Where(a => a.IsActive).ToListAsync(ct);
        var settings = await db.UserSettings.FirstAsync(ct);

        var quotes = await finnhub.GetQuotesBatchAsync(watchlist.Select(w => w.Symbol));

        var newHistories = new List<AlertHistory>();

        foreach (var (symbol, quote) in quotes)
        {
            if (quote == null) continue;

            var item = watchlist.First(w => w.Symbol == symbol);

            // --- Volatility check (compare to last cached price) ---
            if (item.LastKnownPrice.HasValue && item.LastKnownPrice != 0)
            {
                var swingPct = Math.Abs(((double)quote.CurrentPrice - (double)item.LastKnownPrice.Value)
                                         / (double)item.LastKnownPrice.Value * 100.0);

                var volatilityHistory = volatility.Detect(symbol, (decimal)swingPct, quote.CurrentPrice, settings.VolatilityThreshold);
                if (volatilityHistory != null)
                    newHistories.Add(volatilityHistory);
            }

            // --- Update cached price ---
            item.LastKnownPrice       = quote.CurrentPrice;
            item.PreviousClose        = quote.PreviousClose;
            item.LastPercentChange    = quote.PercentChange;
            item.LastPriceUpdatedAt   = DateTime.UtcNow;

            // --- Threshold alerts ---
            var symbolAlerts = alerts.Where(a => a.Symbol == symbol);
            foreach (var alert in symbolAlerts)
            {
                if (Math.Abs(quote.PercentChange) < alert.ThresholdPercent) continue;

                var alertType = quote.PercentChange > 0 ? "SPIKE" : "DROP";
                newHistories.Add(new AlertHistory
                {
                    Symbol         = symbol,
                    PriceAtTrigger = quote.CurrentPrice,
                    PercentChange  = quote.PercentChange,
                    AlertType      = alertType,
                    Notes          = $"Alert #{alert.Id}: threshold {alert.ThresholdPercent}% exceeded",
                    AlertId        = alert.Id
                });

                alert.LastTriggeredAt = DateTime.UtcNow;

                _logger.LogInformation("{Type} alert triggered for {Symbol}: {Pct:F2}%", alertType, symbol, quote.PercentChange);
            }
        }

        db.AlertHistories.AddRange(newHistories);
        await db.SaveChangesAsync(ct);

        // --- Send email notifications for new alerts ---
        if (newHistories.Count > 0 && !string.IsNullOrWhiteSpace(settings.Email))
        {
            await SendAlertEmailAsync(email, settings.Email, newHistories);
        }
    }

    private static async Task SendAlertEmailAsync(IEmailService email, string to, List<AlertHistory> histories)
    {
        var rows = string.Join("", histories.Select(h =>
            $"<tr><td>{h.Symbol}</td><td>{h.AlertType}</td><td>{h.PercentChange:+0.00;-0.00}%</td><td>${h.PriceAtTrigger:F2}</td><td>{h.TriggeredAt:HH:mm UTC}</td></tr>"));

        var html = $"""
            <h2>⚡ Financial Dashboard – Price Alerts</h2>
            <table border="1" cellpadding="6" cellspacing="0">
              <thead><tr><th>Symbol</th><th>Type</th><th>Change</th><th>Price</th><th>Time</th></tr></thead>
              <tbody>{rows}</tbody>
            </table>
            """;

        await email.SendAsync(to, $"Price Alert – {histories.Count} triggered", html);
    }
}
