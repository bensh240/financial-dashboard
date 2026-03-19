using FinancialDashboard.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialDashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewsController : ControllerBase
{
    private readonly IFinnhubService _finnhub;

    public NewsController(IFinnhubService finnhub) => _finnhub = finnhub;

    /// <summary>General market news.</summary>
    [HttpGet("market")]
    public async Task<IActionResult> Market([FromQuery] string category = "general")
    {
        var items = await _finnhub.GetMarketNewsAsync(category);
        return Ok(items);
    }

    /// <summary>Company-specific news for the last 7 days.</summary>
    [HttpGet("stock/{symbol}")]
    public async Task<IActionResult> Stock(string symbol)
    {
        var items = await _finnhub.GetCompanyNewsAsync(symbol);
        return Ok(items);
    }
}
