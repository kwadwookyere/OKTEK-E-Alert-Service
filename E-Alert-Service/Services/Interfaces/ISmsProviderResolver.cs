using EAlertService.Models.DomainModels;

namespace EAlertService.Services.Interfaces;

public interface ISmsProviderResolver
{
    Task<SmsProviderConfiguration?> ResolveSmsProviderAsync(int applicationId, int? requestedPriority = null);
}
