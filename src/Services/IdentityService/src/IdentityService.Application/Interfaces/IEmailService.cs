namespace IdentityService.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public record EmailMessage(
    List<EmailRecipient> To,
    string Subject,
    string HtmlBody,
    string? TextBody = null,
    List<EmailRecipient>? Cc = null,
    List<EmailAttachment>? Attachments = null,
    string? MessageId = null
);

public record EmailRecipient(string Name, string Email);
public record EmailAttachment(string FileName, byte[] Data, string MimeType);
