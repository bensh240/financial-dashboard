namespace FinancialDashboard.API.Models;

public class AlertHistory
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal PriceAtTrigger { get; set; }
    public decimal PercentChange { get; set; }

    /// <summary>SPIKE | DROP | VOLATILITY | THRESHOLD</summary>
    public string AlertType { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public int? AlertId { get; set; }
}
