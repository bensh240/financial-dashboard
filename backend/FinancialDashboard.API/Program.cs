using System.Text;
using FinancialDashboard.API.Data;
using FinancialDashboard.API.Services;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
// Load environment overrides from .env file if present (dev convenience)
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
        var idx = trimmed.IndexOf('=');
        if (idx < 0) continue;
        var key = trimmed[..idx].Trim();
        var val = trimmed[(idx + 1)..].Trim().Trim('"');
        Environment.SetEnvironmentVariable(key, val);
        builder.Configuration[key.Replace("__", ":")] = val;
    }
}

// ── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration["AllowedOrigins"] ?? "http://localhost:5173")
        .AllowAnyMethod()
        .AllowAnyHeader()));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "FinancialDashboard_SuperSecret_Key_2024_AtLeast32Chars!";
var keyBytes  = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = "FinancialDashboard",
            ValidAudience            = "FinancialDashboard",
            IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Database ─────────────────────────────────────────────────────────────────
var dbPath = builder.Configuration["Database:Path"] ?? "financial_dashboard.db";
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite($"Data Source={dbPath}"));

// ── Hangfire ─────────────────────────────────────────────────────────────────
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage($"Data Source=hangfire_{dbPath}"));

builder.Services.AddHangfireServer();

// ── HTTP Clients ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<IFinnhubService, FinnhubService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddHttpClient("Yahoo", c =>
{
    c.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; FinancialDashboard/1.0)");
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient(); // generic IHttpClientFactory for Claude etc.

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddSingleton<VolatilityDetectorService>();
builder.Services.AddScoped<DailyBriefService>();
builder.Services.AddScoped<WeeklyReportService>();
builder.Services.AddTransient<IEmailService, SendGridEmailService>();
builder.Services.AddHostedService<PriceMonitorService>();

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Financial Dashboard API",
        Version     = "v1",
        Description = "Real-time stock monitoring with automated alerts and email reports."
    });

    // JWT Bearer auth in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without the 'Bearer ' prefix)."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Migrate DB ────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // EnsureCreated creates all tables from the model (no migration discovery needed).
    db.Database.EnsureCreated();

    // Add new columns to existing tables via ALTER TABLE (SQLite compatible, try/catch for idempotency)
    try { db.Database.ExecuteSqlRaw("ALTER TABLE UserSettings ADD COLUMN SendGridApiKey TEXT NOT NULL DEFAULT ''"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE UserSettings ADD COLUMN ClaudeApiKey TEXT NOT NULL DEFAULT ''"); } catch { }
    try { db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS PortfolioItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, Symbol TEXT NOT NULL, Quantity TEXT NOT NULL, AvgCostPrice TEXT NOT NULL, AddedAt TEXT NOT NULL, Notes TEXT)"); } catch { }
}

// ── Middleware ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Dashboard API v1"));
}

app.UseCors();
app.UseHangfireDashboard("/hangfire");

app.UseAuthentication();
app.UseAuthorization();

// ── Hangfire Recurring Jobs ───────────────────────────────────────────────────
// Read saved daily brief time (default 08:00 UTC)
using (var scope = app.Services.CreateScope())
{
    var db       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var settings = db.UserSettings.FirstOrDefault();
    var briefTime = settings?.DailyBriefTime ?? "08:00";
    var hour      = TimeOnly.TryParse(briefTime, out var t) ? t.Hour : 8;

    RecurringJob.AddOrUpdate<DailyBriefService>(
        "daily-brief",
        svc => svc.SendDailyBriefAsync(),
        $"0 {hour} * * *"); // daily at configured hour

    RecurringJob.AddOrUpdate<WeeklyReportService>(
        "weekly-report",
        svc => svc.SendWeeklyReportAsync(),
        "0 9 * * 0"); // Sunday 9 AM

    RecurringJob.AddOrUpdate<VolatilityDetectorService>(
        "volatility-summary",
        svc => svc.RunVolatilitySummaryJob(),
        "*/15 * * * *"); // every 15 minutes
}

app.MapControllers();
app.Run();
