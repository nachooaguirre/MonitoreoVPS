namespace VpsMonitor.Web.Infrastructure.Notifications;

using System.Net;
using System.Net.Mail;

public interface IEmailNotificationService
{
    Task SendAlertNotificationAsync(string subject, string body, string recipientEmail, CancellationToken ct = default);
}

public sealed class EmailNotificationService : IEmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAlertNotificationAsync(string subject, string body, string recipientEmail, CancellationToken ct = default)
    {
        var enabled = _configuration.GetValue("Smtp:Enabled", false);
        if (!enabled)
        {
            _logger.LogInformation("SMTP notifications are disabled. Skipping alert email dispatch.");
            return;
        }

        var host = _configuration["Smtp:Host"] ?? "smtp.internal";
        var port = _configuration.GetValue("Smtp:Port", 25);
        var useSsl = _configuration.GetValue("Smtp:UseSsl", false);
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:FromAddress"] ?? "no-reply@vpsmonitor.local";

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(from, "VPS Monitor"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(recipientEmail);

            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = useSsl,
                Credentials = string.IsNullOrWhiteSpace(username) ? null : new NetworkCredential(username, password)
            };

            await smtpClient.SendMailAsync(message, ct);
            _logger.LogInformation("Alert email successfully dispatched to {Recipient}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert notification email to {Recipient}", recipientEmail);
        }
    }
}
