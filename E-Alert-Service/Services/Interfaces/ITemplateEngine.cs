namespace EAlertService.Services.Interfaces;

public interface ITemplateEngine
{
    string Render(string template, IDictionary<string, string> placeholders);
}
