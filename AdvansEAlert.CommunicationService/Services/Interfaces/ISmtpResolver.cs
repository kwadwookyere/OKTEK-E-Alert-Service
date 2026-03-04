using AdvansEAlert.CommunicationService.Models.DomainModels;

namespace AdvansEAlert.CommunicationService.Services.Interfaces;

public interface ISmtpResolver
{
    Task<SmtpConfiguration?> ResolveSmtpConfigurationAsync(int applicationId, int? requestedPriority = null);
}
