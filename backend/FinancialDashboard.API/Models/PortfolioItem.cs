namespace FinancialDashboard.API.Models;

public class PortfolioItem
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AvgCostPrice { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
