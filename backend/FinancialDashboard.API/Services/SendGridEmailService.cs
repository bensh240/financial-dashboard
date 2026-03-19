using SendGrid;
using SendGrid.Helpers.Mail;

namespace FinancialDashboard.API.Services;

public class SendGridEmailService : IEmailService
{
    private readonly SendGridClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration cfg, ILogger<SendGridEmailService> logger)
    {
        _logger    = logger;
        var apiKey = cfg["SendGrid:ApiKey"] ?? "";
        _fromEmail = cfg["SendGrid:FromEmail"] ?? "noreply@financialdashboard.local";
        _fromName  = cfg["SendGrid:FromName"]  ?? "Financial Dashboard";
        if (string.IsNullOrWhiteSpace(apiKey))
            _logger.LogWarning("SendGrid:ApiKey is not configured – emails will be skipped.");
        _client = new SendGridClient(string.IsNullOrWhiteSpace(apiKey) ? "placeholder" : apiKey);
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("Email not sent – recipient address is empty. Configure it in Settings.");
            return;
        }

        var msg = MailHelper.CreateSingleEmail(
            from:    new EmailAddress(_fromEmail, _fromName),
            to:      new EmailAddress(to),
            subject: subject,
            plainTextContent: null,
            htmlContent: htmlBody
        );

        var response = await _client.SendEmailAsync(msg);

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
