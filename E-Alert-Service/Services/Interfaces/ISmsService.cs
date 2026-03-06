namespace EAlertService.Services.Interfaces;

public interface ISmsService
{
    Task<bool> SendSmsAsync(int applicationId, string recipient, string message, int? priority = null);
    Task<bool> SendSmsWithTemplateAsync(int applicationId, string recipient, string templateName, IDictionary<string, string> placeholders, int? priority = null);
}
