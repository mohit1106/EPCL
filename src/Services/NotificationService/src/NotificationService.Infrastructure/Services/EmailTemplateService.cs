using System.Reflection;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Loads embedded HTML templates and replaces {{Variable}} placeholders.
/// Templates stored in NotificationService.Infrastructure/Email/Templates/*.html
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly Dictionary<string, string> _templateCache = new();

    public string Render(string templateName, Dictionary<string, string> variables)
    {
        var template = LoadTemplate(templateName);
        foreach (var (key, value) in variables)
        {
            template = template.Replace($"{{{{{key}}}}}", value);
        }
        return template;
    }

    private string LoadTemplate(string templateName)
    {
        if (_templateCache.TryGetValue(templateName, out var cached))
            return cached;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"NotificationService.Infrastructure.Email.Templates.{templateName}.html";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Fallback: return a simple wrapper
            return BuildFallbackTemplate(templateName);
        }

        using var reader = new StreamReader(stream);
        var template = reader.ReadToEnd();
        _templateCache[templateName] = template;
        return template;
    }

    private static string BuildFallbackTemplate(string templateName) =>
        """
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"><title>EPCL Notification</title></head>
        <body style="margin:0;padding:0;background-color:#0F172A;font-family:'Helvetica Neue',Arial,sans-serif;">
        <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#0F172A;padding:40px 20px;">
          <tr><td align="center">
            <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">
              <tr><td style="background:linear-gradient(135deg,#1B3A6B 0%,#2563EB 100%);padding:30px 40px;border-radius:16px 16px 0 0;text-align:center;">
                <span style="font-size:36px;font-weight:900;color:#FFF;letter-spacing:-1px;">EPCL</span>
                <div style="font-size:11px;color:#93C5FD;letter-spacing:3px;text-transform:uppercase;margin-top:4px;">Eleven Petroleum Corporation Limited</div>
              </td></tr>
              <tr><td style="background:#FFFFFF;padding:40px;border-radius:0 0 16px 16px;">
                <h1 style="margin:0 0 16px;font-size:22px;color:#0F172A;">{{Subject}}</h1>
                <div style="font-size:15px;color:#334155;line-height:1.7;">{{Content}}</div>
              </td></tr>
              <tr><td style="padding:24px 0;text-align:center;">
                <p style="margin:0;font-size:12px;color:#475569;">© 2025 Eleven Petroleum Corporation Limited. All rights reserved.</p>
              </td></tr>
            </table>
          </td></tr>
        </table>
        </body>
        </html>
        """;
}
