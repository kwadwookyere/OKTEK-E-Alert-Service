using AdvansEAlert.CommunicationService.Data;
using AdvansEAlert.CommunicationService.Models.DomainModels;
using AdvansEAlert.CommunicationService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdvansEAlert.CommunicationService.Services.Implementations;

public class SmsProviderResolver : ISmsProviderResolver
{
    private readonly ApplicationDbContext _context;

    public SmsProviderResolver(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SmsProviderConfiguration?> ResolveSmsProviderAsync(int applicationId, int? requestedPriority = null)
    {
        var query = _context.SmsProviders
            .Where(s => s.ApplicationId == applicationId && s.IsActive);

        if (requestedPriority.HasValue)
        {
            query = query.Where(s => s.Priority == requestedPriority.Value);
        }

        return await query
            .OrderBy(s => s.Priority)
            .FirstOrDefaultAsync();
    }
}
