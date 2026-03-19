using FinancialDashboard.API.Data;
using FinancialDashboard.API.DTOs;
using FinancialDashboard.API.Models;
using FinancialDashboard.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialDashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WatchlistController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFinnhubService _finnhub;

    public WatchlistController(AppDbContext db, IFinnhubService finnhub)
    {
        _db      = db;
        _finnhub = finnhub;
    }

    /// <summary>Get all watchlist items with live prices.</summary>
    [HttpGet]
    public async Task<ActionResult<List<WatchlistItemDto>>> GetAll()
    {
        var items = await _db.WatchlistItems.OrderBy(w => w.Symbol).ToListAsync();
        var result = new List<WatchlistItemDto>();

        foreach (var item in items)
        {
            var quote = await _finnhub.GetQuoteAsync(item.Symbol);

            if (quote != null)
            {
                item.LastKnownPrice     = quote.CurrentPrice;
                item.PreviousClose      = quote.PreviousClose;
                item.LastPercentChange  = quote.PercentChange;
                item.LastPriceUpdatedAt = DateTime.UtcNow;
            }

            result.Add(MapToDto(item, quote));
        }

        await _db.SaveChangesAsync();
        return Ok(result);
    }

    /// <summary>Add a stock to the watchlist.</summary>
    [HttpPost]
    public async Task<ActionResult<WatchlistItemDto>> Add([FromBody] AddWatchlistRequest request)
    {
        var symbol = request.Symbol.ToUpperInvariant().Trim();

        if (await _db.WatchlistItems.AnyAsync(w => w.Symbol == symbol))
            return Conflict(new { error = $"{symbol} is already in the watchlist." });

        // Validate via Finnhub
        var quote = await _finnhub.GetQuoteAsync(symbol);
        if (quote == null)
            return BadRequest(new { error = $"Could not find a valid quote for '{symbol}'. Check the symbol and try again." });

        var profile = await _finnhub.GetCompanyProfileAsync(symbol);

        var item = new WatchlistItem
        {
            Symbol              = symbol,
            CompanyName         = profile?.Name,
            LastKnownPrice      = quote.CurrentPrice,
            PreviousClose       = quote.PreviousClose,
            LastPercentChange   = quote.PercentChange,
            LastPriceUpdatedAt  = DateTime.UtcNow
        };

        _db.WatchlistItems.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), null, MapToDto(item, quote));
    }

    /// <summary>Remove a stock from the watchlist.</summary>
    [HttpDelete("{symbol}")]
    public async Task<IActionResult> Remove(string symbol)
    {
        var sym  = symbol.ToUpperInvariant();
        var item = await _db.WatchlistItems.FirstOrDefaultAsync(w => w.Symbol == sym);

        if (item == null) return NotFound(new { error = $"{sym} not found in watchlist." });

        _db.WatchlistItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static WatchlistItemDto MapToDto(WatchlistItem item, FinnhubQuote? quote) => new(
        item.Id,
        item.Symbol,
        item.CompanyName,
        quote?.CurrentPrice ?? item.LastKnownPrice,
        quote?.Change,
        quote?.PercentChange ?? item.LastPercentChange,
        quote?.PreviousClose ?? item.PreviousClose,
        item.AddedAt,
        item.LastPriceUpdatedAt
    );
}
