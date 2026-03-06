using EAlertService.Models.DomainModels;

namespace EAlertService.Services.Interfaces;

public interface ISmtpResolver
{
    Task<SmtpConfiguration?> ResolveSmtpConfigurationAsync(int applicationId, int? requestedPriority = null);
}
