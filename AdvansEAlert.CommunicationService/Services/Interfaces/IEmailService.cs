using AdvansEAlert.CommunicationService.Models.DomainModels;

namespace AdvansEAlert.CommunicationService.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(int applicationId, string recipient, string subject, string body, int? priority = null);
    Task<bool> SendEmailWithTemplateAsync(int applicationId, string recipient, string templateName, IDictionary<string, string> placeholders, int? priority = null);
    Task<(bool Success, string Message)> TestConnectionAsync(SmtpConfiguration config, string password);
}

