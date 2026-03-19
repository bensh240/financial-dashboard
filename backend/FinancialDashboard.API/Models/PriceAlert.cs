namespace FinancialDashboard.API.Models;

public class PriceAlert
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Trigger when |% change| exceeds this value</summary>
    public decimal ThresholdPercent { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAt { get; set; }
}
