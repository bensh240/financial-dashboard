namespace FinancialDashboard.API.DTOs;

public record SettingsDto(
    string  Email,
    string  DailyBriefTime,
    bool    DailyBriefEnabled,
    bool    WeeklyReportEnabled,
    decimal VolatilityThreshold
);

public record UpdateSettingsRequest(
    string  Email,
    string  DailyBriefTime,
    bool    DailyBriefEnabled,
    bool    WeeklyReportEnabled,
    decimal VolatilityThreshold
);
