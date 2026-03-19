using FinancialDashboard.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialDashboard.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<WatchlistItem> WatchlistItems { get; set; }
    public DbSet<PriceAlert>    PriceAlerts    { get; set; }
    public DbSet<AlertHistory>  AlertHistories  { get; set; }
    public DbSet<UserSettings>  UserSettings    { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed default settings row
        modelBuilder.Entity<UserSettings>().HasData(new UserSettings
        {
            Id                   = 1,
            Email                = "",
            DailyBriefTime       = "08:00",
            DailyBriefEnabled    = true,
            WeeklyReportEnabled  = true,
            VolatilityThreshold  = 3.0m
        });
    }
}
