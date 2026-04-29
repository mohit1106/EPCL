using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Gmail SMTP email service using MailKit — identical pattern to Identity Service.
/// Uses env vars: SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS, SMTP_FROM_EMAIL, SMTP_FROM_NAME.
/// </summary>
public class MailKitEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IConfiguration config, ILogger<MailKitEmailService> logger)
    { _config = config; _logger = logger; }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["SMTP_FROM_NAME"] ?? "EPCL Fuel Platform",
            _config["SMTP_FROM_ADDRESS"] ?? "noreply@epcl.in"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _config["SMTP_HOST"] ?? "smtp.gmail.com",
            int.Parse(_config["SMTP_PORT"] ?? "587"),
            MailKit.Security.SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(
            _config["SMTP_USER"] ?? "",
            _config["SMTP_PASS"] ?? "", ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
    }
}

/// <summary>
/// SMS Service stub — logs the message. Replace with MSG91 or Twilio in production.
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    public SmsService(ILogger<SmsService> logger) { _logger = logger; }

    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        // In production: POST to MSG91 API or Twilio
        _logger.LogInformation("SMS → {Phone}: {Message}", phoneNumber, message.Length > 60 ? message[..60] + "..." : message);
        return Task.CompletedTask;
    }
}
