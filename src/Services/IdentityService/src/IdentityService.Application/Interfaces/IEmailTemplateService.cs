namespace IdentityService.Application.Interfaces;

public interface IEmailTemplateService
{
    string Render(string templateName, Dictionary<string, string> variables);
}
