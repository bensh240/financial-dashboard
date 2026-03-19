using FinancialDashboard.API.Models;

namespace FinancialDashboard.API.Services;

public interface IFinnhubService
{
    Task<FinnhubQuote?>   GetQuoteAsync(string symbol);
    Task<FinnhubProfile?> GetCompanyProfileAsync(string symbol);
    Task<FinnhubCandle?>  GetCandlesAsync(string symbol, string resolution, long from, long to);
    Task<List<(string Symbol, FinnhubQuote? Quote)>> GetQuotesBatchAsync(IEnumerable<string> symbols);
    Task<FinnhubSearchResponse?> SearchSymbolsAsync(string query);
}
