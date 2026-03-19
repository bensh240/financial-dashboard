namespace FinancialDashboard.API.DTOs;

public record SettingsDto(
    string  Email,
    string  DailyBriefTime,
    bool    DailyBriefEnabled,
    bool    WeeklyReportEnabled,
    decimal VolatilityThreshold,
    string  AdminUsername
);

public record UpdateSettingsRequest(
    string  Email,
    string  DailyBriefTime,
    bool    DailyBriefEnabled,
    bool    WeeklyReportEnabled,
    decimal VolatilityThreshold,
    string  AdminUsername,
    string? AdminPassword   // null/empty = keep current password
);
