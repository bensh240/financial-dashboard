namespace FinancialDashboard.API.DTOs;

public record CreateAlertRequest(string Symbol, decimal ThresholdPercent);

public record UpdateAlertRequest(decimal ThresholdPercent, bool IsActive);

public record AlertDto(
    int      Id,
    string   Symbol,
    decimal  ThresholdPercent,
    bool     IsActive,
    DateTime CreatedAt,
    DateTime? LastTriggeredAt
);

public record AlertHistoryDto(
    int      Id,
    string   Symbol,
    decimal  PriceAtTrigger,
    decimal  PercentChange,
    string   AlertType,
    string?  Notes,
    DateTime TriggeredAt,
    int?     AlertId
);
