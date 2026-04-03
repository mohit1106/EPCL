using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using IdentityService.Application.Interfaces;

namespace IdentityService.Infrastructure.Email;

public class GmailSmtpEmailService(
    IOptions<EmailSettings> settings,
    ILogger<GmailSmtpEmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var mime = new MimeMessage();

        mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));

        foreach (var recipient in message.To)
            mime.To.Add(new MailboxAddress(recipient.Name, recipient.Email));

        if (message.Cc != null)
            foreach (var cc in message.Cc)
                mime.Cc.Add(new MailboxAddress(cc.Name, cc.Email));

        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        };

        if (message.Attachments != null)
            foreach (var attachment in message.Attachments)
                builder.Attachments.Add(attachment.FileName, attachment.Data, ContentType.Parse(attachment.MimeType));

        mime.Body = builder.ToMessageBody();
        mime.Headers.Add("X-EPCL-MessageId", message.MessageId ?? Guid.NewGuid().ToString());

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
            await client.SendAsync(mime, ct);

            logger.LogInformation(
                "Email sent successfully. To: {Recipients}, Subject: {Subject}",
                string.Join(", ", message.To.Select(r => r.Email)),
                message.Subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email. Subject: {Subject}", message.Subject);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}

public class EmailSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
}
