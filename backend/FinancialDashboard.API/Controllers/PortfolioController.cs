using FinancialDashboard.API.Data;
using FinancialDashboard.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialDashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(AppDbContext db, ILogger<PortfolioController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public record CreatePortfolioRequest(string Symbol, decimal Quantity, decimal AvgCostPrice, string? Notes);
    public record UpdatePortfolioRequest(decimal Quantity, decimal AvgCostPrice, string? Notes);

    private decimal GetCurrentPrice(string symbol, List<WatchlistItem> watchlist)
    {
        var item = watchlist.FirstOrDefault(w => w.Symbol == symbol.ToUpperInvariant());
        return item?.LastKnownPrice ?? 0m;
    }

    private object MapItem(PortfolioItem item, decimal currentPrice)
    {
        var invested     = item.Quantity * item.AvgCostPrice;
        var currentValue = item.Quantity * currentPrice;
        var plDollar     = currentValue - invested;
        var plPercent    = invested == 0 ? 0 : (plDollar / invested) * 100;

        return new
        {
            item.Id,
            item.Symbol,
            item.Quantity,
            item.AvgCostPrice,
            item.AddedAt,
            item.Notes,
            currentPrice,
            currentValue,
            plDollar,
            plPercent
        };
    }

    /// <summary>Get all portfolio items with current price and P&L.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items     = await _db.PortfolioItems.OrderBy(p => p.Symbol).ToListAsync();
        var watchlist = await _db.WatchlistItems.ToListAsync();

        var result = items.Select(item =>
        {
            var price = GetCurrentPrice(item.Symbol, watchlist);
            return MapItem(item, price);
        }).ToList();

        return Ok(result);
    }

    /// <summary>Add a portfolio position.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePortfolioRequest req)
    {
        var item = new PortfolioItem
        {
            Symbol       = req.Symbol.ToUpperInvariant().Trim(),
            Quantity     = req.Quantity,
            AvgCostPrice = req.AvgCostPrice,
            Notes        = req.Notes,
            AddedAt      = DateTime.UtcNow
        };

        _db.PortfolioItems.Add(item);
        await _db.SaveChangesAsync();

        var watchlist    = await _db.WatchlistItems.ToListAsync();
        var currentPrice = GetCurrentPrice(item.Symbol, watchlist);

        return CreatedAtAction(nameof(GetAll), null, MapItem(item, currentPrice));
    }

    /// <summary>Update a portfolio position.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePortfolioRequest req)
    {
        var item = await _db.PortfolioItems.FindAsync(id);
        if (item == null) return NotFound(new { error = $"Portfolio item {id} not found." });

        item.Quantity     = req.Quantity;
        item.AvgCostPrice = req.AvgCostPrice;
        item.Notes        = req.Notes;

        await _db.SaveChangesAsync();

        var watchlist    = await _db.WatchlistItems.ToListAsync();
        var currentPrice = GetCurrentPrice(item.Symbol, watchlist);

        return Ok(MapItem(item, currentPrice));
    }

    /// <summary>Delete a portfolio position.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.PortfolioItems.FindAsync(id);
        if (item == null) return NotFound(new { error = $"Portfolio item {id} not found." });

        _db.PortfolioItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get portfolio summary: total invested, current value, P&L.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var items     = await _db.PortfolioItems.ToListAsync();
        var watchlist = await _db.WatchlistItems.ToListAsync();

        var totalInvested     = 0m;
        var totalCurrentValue = 0m;

        foreach (var item in items)
        {
            var price = GetCurrentPrice(item.Symbol, watchlist);
            totalInvested     += item.Quantity * item.AvgCostPrice;
            totalCurrentValue += item.Quantity * price;
        }

        var totalPlDollar  = totalCurrentValue - totalInvested;
        var totalPlPercent = totalInvested == 0 ? 0 : (totalPlDollar / totalInvested) * 100;

        return Ok(new
        {
            totalInvested,
            totalCurrentValue,
            totalPlDollar,
            totalPlPercent
        });
    }
}
