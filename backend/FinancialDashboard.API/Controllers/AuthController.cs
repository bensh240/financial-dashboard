using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinancialDashboard.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FinancialDashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;
    private readonly AppDbContext _db;

    public AuthController(IConfiguration config, ILogger<AuthController> logger, AppDbContext db)
    {
        _config = config;
        _logger = logger;
        _db = db;
    }

    public record LoginRequest(string Username, string Password);

    /// <summary>Login with username/password, returns a JWT token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var adminUser = _config["Admin:Username"] ?? "admin";
        var adminPass = _config["Admin:Password"] ?? "admin123";

        // DB-stored credentials override appsettings defaults
        var dbSettings = await _db.UserSettings.FirstOrDefaultAsync();
        if (dbSettings != null)
        {
            if (!string.IsNullOrWhiteSpace(dbSettings.AdminUsername))
                adminUser = dbSettings.AdminUsername;
            if (!string.IsNullOrWhiteSpace(dbSettings.AdminPassword))
                adminPass = dbSettings.AdminPassword;
        }

        if (!string.Equals(req.Username, adminUser, StringComparison.OrdinalIgnoreCase) ||
            req.Password != adminPass)
        {
            return Unauthorized(new { error = "Invalid username or password." });
        }

        var token = GenerateJwtToken(req.Username);
        _logger.LogInformation("User '{Username}' logged in", req.Username);

        return Ok(new { token, username = req.Username });
    }

    /// <summary>Logout (client should discard the token).</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout() => Ok(new { message = "Logged out." });

    /// <summary>Get current authenticated user info from JWT claims.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var username = User.Identity?.Name
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? "unknown";

        return Ok(new { username });
    }

    private string GenerateJwtToken(string username)
    {
        var secret  = _config["Jwt:Secret"] ?? "FinancialDashboard_SuperSecret_Key_2024_AtLeast32Chars!";
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(7);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             "FinancialDashboard",
            audience:           "FinancialDashboard",
            claims:             claims,
            expires:            expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
