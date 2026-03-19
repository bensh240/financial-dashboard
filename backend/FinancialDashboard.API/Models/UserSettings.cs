namespace FinancialDashboard.API.Models;

public class UserSettings
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>HH:mm local time for daily brief</summary>
    public string DailyBriefTime { get; set; } = "08:00";

    public bool DailyBriefEnabled { get; set; } = true;
    public bool WeeklyReportEnabled { get; set; } = true;

    /// <summary>% swing in a single polling cycle that triggers a volatility alert</summary>
    public decimal VolatilityThreshold { get; set; } = 3.0m;

    public string SendGridApiKey { get; set; } = string.Empty;
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
