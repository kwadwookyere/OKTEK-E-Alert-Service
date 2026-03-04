using AdvansEAlert.CommunicationService.Models.DomainModels;

namespace AdvansEAlert.CommunicationService.Services.Interfaces;

public interface ISmsProviderResolver
{
    Task<SmsProviderConfiguration?> ResolveSmsProviderAsync(int applicationId, int? requestedPriority = null);
}
