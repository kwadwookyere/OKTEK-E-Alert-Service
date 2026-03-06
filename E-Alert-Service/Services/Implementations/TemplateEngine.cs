using EAlertService.Services.Interfaces;
using System.Text.RegularExpressions;

namespace EAlertService.Services.Implementations;

public class TemplateEngine : ITemplateEngine
{
    public string Render(string template, IDictionary<string, string> placeholders)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;
        if (placeholders == null || placeholders.Count == 0) return template;

        string rendered = template;
        foreach (var placeholder in placeholders)
        {
            string pattern = $@"{{{{\s*{Regex.Escape(placeholder.Key)}\s*}}}}";
            rendered = Regex.Replace(rendered, pattern, placeholder.Value ?? string.Empty, RegexOptions.IgnoreCase);
        }

        return rendered;
    }
}
