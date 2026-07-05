using System.Net;
using System.Net.Mail;

namespace AdopcionDbAPI.Services.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Email:Smtp:Host"];
        var from = _configuration["Email:From"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogInformation(
                "Email not sent because SMTP is not configured. To: {To}. Subject: {Subject}",
                message.To,
                message.Subject
            );
            return;
        }

        var port = int.TryParse(_configuration["Email:Smtp:Port"], out var configuredPort)
            ? configuredPort
            : 587;
        var enableSsl = bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var configuredSsl)
            ? configuredSsl
            : true;
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(from, _configuration["Email:FromName"] ?? "PetAdopt"),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false
        };
        mailMessage.To.Add(message.To);

        using var smtpClient = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            smtpClient.Credentials = new NetworkCredential(username, password);
        }

        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }
}
