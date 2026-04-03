using System.Reflection;
using IdentityService.Application.Interfaces;

namespace IdentityService.Infrastructure.Email;

public class EmailTemplateService : IEmailTemplateService
{
    public string Render(string templateName, Dictionary<string, string> variables)
    {
        var assembly = typeof(EmailTemplateService).Assembly;
        var resourceName = $"IdentityService.Infrastructure.Email.Templates.{templateName}.html";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Email template '{templateName}' not found as embedded resource.");
        using var reader = new StreamReader(stream);
        var template = reader.ReadToEnd();

        foreach (var (key, value) in variables)
            template = template.Replace($"{{{{{key}}}}}", value);

        return template;
    }
}
