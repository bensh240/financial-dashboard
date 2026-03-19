namespace FinancialDashboard.API.Models;

public class WatchlistItem
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public decimal? LastKnownPrice { get; set; }
    public decimal? PreviousClose { get; set; }
    public decimal? LastPercentChange { get; set; }
    public DateTime? LastPriceUpdatedAt { get; set; }
}
