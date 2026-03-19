using FinancialDashboard.API.Data;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FinancialDashboard.API.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IServiceProvider services, IConfiguration config, ILogger<SendGridEmailService> logger)
    {
        _services  = services;
        _config    = config;
        _logger    = logger;
        _fromEmail = config["SendGrid:FromEmail"] ?? "noreply@financialdashboard.local";
        _fromName  = config["SendGrid:FromName"]  ?? "Financial Dashboard";
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("Email not sent – recipient address is empty. Configure it in Settings.");
            return;
        }

        // Prefer UserSettings key, fall back to appsettings
        string apiKey = "";
        try
        {
            using var scope = _services.CreateScope();
            var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var settings    = await db.UserSettings.FirstOrDefaultAsync();
            if (settings != null && !string.IsNullOrWhiteSpace(settings.SendGridApiKey))
                apiKey = settings.SendGridApiKey;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read SendGrid key from UserSettings, falling back to appsettings");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = _config["SendGrid:ApiKey"] ?? "";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("SendGrid:ApiKey is not configured – emails will be skipped.");
            return;
        }

        var client = new SendGridClient(apiKey);

        var msg = MailHelper.CreateSingleEmail(
            from:    new EmailAddress(_fromEmail, _fromName),
            to:      new EmailAddress(to),
            subject: subject,
            plainTextContent: null,
            htmlContent: htmlBody
        );

        var response = await client.SendEmailAsync(msg);

        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid error {Code}: {Body}", response.StatusCode, body);
        }
        else
        {
            _logger.LogInformation("Email sent to {To} – Subject: {Subject}", to, subject);
        }
    }
}
