using FinancialDashboard.API.Data;
using FinancialDashboard.API.DTOs;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialDashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db) => _db = db;

    /// <summary>Get current user settings.</summary>
    [HttpGet]
    public async Task<ActionResult<SettingsDto>> Get()
    {
        var s = await _db.UserSettings.FirstAsync();
        return Ok(new SettingsDto(
            s.Email,
            s.DailyBriefTime,
            s.DailyBriefEnabled,
            s.WeeklyReportEnabled,
            s.VolatilityThreshold,
            s.SendGridApiKey,
            s.ClaudeApiKey));
    }

    /// <summary>Update user settings and reschedule Hangfire jobs.</summary>
    [HttpPut]
    public async Task<ActionResult<SettingsDto>> Update([FromBody] UpdateSettingsRequest req)
    {
        if (!TimeOnly.TryParse(req.DailyBriefTime, out var parsed))
            return BadRequest(new { error = "DailyBriefTime must be in HH:mm format." });

        var s = await _db.UserSettings.FirstAsync();
        s.Email                = req.Email;
        s.DailyBriefTime       = req.DailyBriefTime;
        s.DailyBriefEnabled    = req.DailyBriefEnabled;
        s.WeeklyReportEnabled  = req.WeeklyReportEnabled;
        s.VolatilityThreshold  = req.VolatilityThreshold;
        s.SendGridApiKey       = req.SendGridApiKey;
        s.ClaudeApiKey         = req.ClaudeApiKey;

        await _db.SaveChangesAsync();

        // Reschedule daily brief with new time (UTC cron)
        var cron = $"0 {parsed.Hour} * * *";
        RecurringJob.AddOrUpdate<Services.DailyBriefService>(
            "daily-brief",
            svc => svc.SendDailyBriefAsync(),
            cron);

        return Ok(new SettingsDto(
            s.Email,
            s.DailyBriefTime,
            s.DailyBriefEnabled,
            s.WeeklyReportEnabled,
            s.VolatilityThreshold,
            s.SendGridApiKey,
            s.ClaudeApiKey));
    }
}
