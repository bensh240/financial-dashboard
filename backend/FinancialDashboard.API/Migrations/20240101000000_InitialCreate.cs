using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialDashboard.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertHistories",
                columns: table => new
                {
                    Id             = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Symbol         = table.Column<string>(type: "TEXT", nullable: false),
                    PriceAtTrigger = table.Column<decimal>(type: "TEXT", nullable: false),
                    PercentChange  = table.Column<decimal>(type: "TEXT", nullable: false),
                    AlertType      = table.Column<string>(type: "TEXT", nullable: false),
                    Notes          = table.Column<string>(type: "TEXT", nullable: true),
                    TriggeredAt    = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AlertId        = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AlertHistories", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id               = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Symbol           = table.Column<string>(type: "TEXT", nullable: false),
                    ThresholdPercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive         = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt        = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastTriggeredAt  = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_PriceAlerts", x => x.Id));

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id                  = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Email               = table.Column<string>(type: "TEXT", nullable: false),
                    DailyBriefTime      = table.Column<string>(type: "TEXT", nullable: false),
                    DailyBriefEnabled   = table.Column<bool>(type: "INTEGER", nullable: false),
                    WeeklyReportEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    VolatilityThreshold = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_UserSettings", x => x.Id));

            migrationBuilder.CreateTable(
                name: "WatchlistItems",
                columns: table => new
                {
                    Id                  = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Symbol              = table.Column<string>(type: "TEXT", nullable: false),
                    CompanyName         = table.Column<string>(type: "TEXT", nullable: true),
                    AddedAt             = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastKnownPrice      = table.Column<decimal>(type: "TEXT", nullable: true),
                    PreviousClose       = table.Column<decimal>(type: "TEXT", nullable: true),
                    LastPercentChange   = table.Column<decimal>(type: "TEXT", nullable: true),
                    LastPriceUpdatedAt  = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_WatchlistItems", x => x.Id));

            migrationBuilder.InsertData(
                table: "UserSettings",
                columns: new[] { "Id", "DailyBriefEnabled", "DailyBriefTime", "Email", "VolatilityThreshold", "WeeklyReportEnabled" },
                values: new object[] { 1, true, "08:00", "", 3.0m, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AlertHistories");
            migrationBuilder.DropTable(name: "PriceAlerts");
            migrationBuilder.DropTable(name: "UserSettings");
            migrationBuilder.DropTable(name: "WatchlistItems");
        }
    }
}
