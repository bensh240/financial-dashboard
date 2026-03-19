using FinancialDashboard.API.Data;
using FinancialDashboard.API.DTOs;
using FinancialDashboard.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialDashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AlertsController(AppDbContext db) => _db = db;

    /// <summary>Get all configured price alerts.</summary>
    [HttpGet]
    public async Task<ActionResult<List<AlertDto>>> GetAlerts() =>
        Ok((await _db.PriceAlerts.OrderBy(a => a.Symbol).ToListAsync()).Select(MapAlert));

    /// <summary>Create a new price alert.</summary>
    [HttpPost]
    public async Task<ActionResult<AlertDto>> CreateAlert([FromBody] CreateAlertRequest req)
    {
        var sym = req.Symbol.ToUpperInvariant().Trim();

        if (!await _db.WatchlistItems.AnyAsync(w => w.Symbol == sym))
            return BadRequest(new { error = $"{sym} is not in the watchlist. Add it first." });

        var alert = new PriceAlert
        {
            Symbol          = sym,
            ThresholdPercent = req.ThresholdPercent
        };

        _db.PriceAlerts.Add(alert);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAlerts), null, MapAlert(alert));
    }

    /// <summary>Update a price alert's threshold or active state.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AlertDto>> UpdateAlert(int id, [FromBody] UpdateAlertRequest req)
    {
        var alert = await _db.PriceAlerts.FindAsync(id);
        if (alert == null) return NotFound(new { error = $"Alert {id} not found." });

        alert.ThresholdPercent = req.ThresholdPercent;
        alert.IsActive         = req.IsActive;
        await _db.SaveChangesAsync();
        return Ok(MapAlert(alert));
    }

    /// <summary>Delete a price alert.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAlert(int id)
    {
        var alert = await _db.PriceAlerts.FindAsync(id);
        if (alert == null) return NotFound(new { error = $"Alert {id} not found." });

        _db.PriceAlerts.Remove(alert);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get alert trigger history, optionally filtered by symbol.</summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<AlertHistoryDto>>> GetHistory([FromQuery] string? symbol, [FromQuery] int limit = 100)
    {
        var query = _db.AlertHistories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(symbol))
            query = query.Where(h => h.Symbol == symbol.ToUpperInvariant());

        var results = await query
            .OrderByDescending(h => h.TriggeredAt)
            .Take(limit)
            .ToListAsync();

        return Ok(results.Select(MapHistory));
    }

    private static AlertDto MapAlert(PriceAlert a) => new(
        a.Id, a.Symbol, a.ThresholdPercent, a.IsActive, a.CreatedAt, a.LastTriggeredAt);

    private static AlertHistoryDto MapHistory(AlertHistory h) => new(
        h.Id, h.Symbol, h.PriceAtTrigger, h.PercentChange, h.AlertType, h.Notes, h.TriggeredAt, h.AlertId);
}
