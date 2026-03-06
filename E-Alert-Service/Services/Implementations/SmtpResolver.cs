using EAlertService.Data;
using EAlertService.Models.DomainModels;
using EAlertService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EAlertService.Services.Implementations;

public class SmtpResolver : ISmtpResolver
{
    private readonly ApplicationDbContext _context;

    public SmtpResolver(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SmtpConfiguration?> ResolveSmtpConfigurationAsync(int applicationId, int? requestedPriority = null)
    {
        var query = _context.SmtpConfigurations
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
