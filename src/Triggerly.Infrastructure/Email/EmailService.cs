using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Triggerly.Application.Interfaces;

namespace Triggerly.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var enabled = bool.TryParse(_configuration["Email:Enabled"], out var e) && e;

        if (!enabled)
        {
            _logger.LogInformation(
                "[Email dev-mode] To: {To} | Subject: {Subject}\n{Body}",
                to, subject, htmlBody);
            return;
        }

        var host = _configuration["Email:SmtpHost"]
            ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");
        var port = int.TryParse(_configuration["Email:SmtpPort"], out var p) ? p : 587;
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@triggerly.io";
        var fromName = _configuration["Email:FromName"] ?? "Triggerly";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = !string.IsNullOrEmpty(username)
                ? new NetworkCredential(username, password)
                : null
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}
