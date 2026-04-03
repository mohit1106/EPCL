namespace NotificationService.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}

public interface IEmailTemplateService
{
    string Render(string templateName, Dictionary<string, string> variables);
}
