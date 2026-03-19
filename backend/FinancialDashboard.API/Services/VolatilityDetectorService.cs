using FinancialDashboard.API.Models;

namespace FinancialDashboard.API.Services;

/// <summary>
/// Detects abnormal intra-cycle price swings.
/// Called by PriceMonitorService; registered as a singleton so the price history
/// dictionary persists across polling cycles.
/// Also registered as a Hangfire recurring job (every 15 min) for logging summaries.
/// </summary>
public class VolatilityDetectorService
{
    private readonly ILogger<VolatilityDetectorService> _logger;

    // Symbol → last alert time (avoids spam)
    private readonly Dictionary<string, DateTime> _lastAlerted = new();
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(30);

    public VolatilityDetectorService(ILogger<VolatilityDetectorService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Evaluates a single price swing. Returns an AlertHistory entry if the swing
    /// exceeds the threshold and the symbol is not in cooldown; otherwise null.
    /// </summary>
    public AlertHistory? Detect(string symbol, decimal swingPct, decimal currentPrice, decimal threshold)
    {
        if (swingPct < threshold) return null;

        if (_lastAlerted.TryGetValue(symbol, out var last)
            && DateTime.UtcNow - last < CooldownPeriod)
        {
            return null; // still in cooldown
        }

        _lastAlerted[symbol] = DateTime.UtcNow;
        _logger.LogWarning("VOLATILITY detected for {Symbol}: {Swing:F2}% swing (threshold {Threshold}%)", symbol, swingPct, threshold);

        return new AlertHistory
        {
            Symbol         = symbol,
            PriceAtTrigger = currentPrice,
            PercentChange  = swingPct,
            AlertType      = "VOLATILITY",
            Notes          = $"Intra-cycle swing of {swingPct:F2}% detected (threshold {threshold}%)"
        };
    }

    /// <summary>Hangfire job – logs a summary of recently volatile symbols.</summary>
    public void RunVolatilitySummaryJob()
    {
        var recent = _lastAlerted
            .Where(kv => DateTime.UtcNow - kv.Value < TimeSpan.FromMinutes(15))
            .Select(kv => kv.Key)
            .ToList();

        if (recent.Count > 0)
            _logger.LogInformation("Volatility summary – recently flagged: {Symbols}", string.Join(", ", recent));
        else
            _logger.LogInformation("Volatility summary – no abnormal swings in last 15 minutes");
    }
}
