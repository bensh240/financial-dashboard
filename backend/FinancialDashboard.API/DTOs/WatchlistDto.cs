namespace FinancialDashboard.API.DTOs;

public record AddWatchlistRequest(string Symbol);

public record WatchlistItemDto(
    int     Id,
    string  Symbol,
    string? CompanyName,
    decimal? Price,
    decimal? Change,
    decimal? PercentChange,
    decimal? PreviousClose,
    DateTime AddedAt,
    DateTime? LastUpdatedAt
);
