using EAlertService.Data;
using EAlertService.Models.DomainModels;
using EAlertService.Services.Interfaces;

namespace EAlertService.Services.Implementations;

// Bug fix #13: IAuditService interface moved to Services.Interfaces namespace (IAuditService.cs).
public class AuditService : IAuditService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AuditService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task LogAsync(string userId, string action, string entity, int? entityId, string details, string ipAddress)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();
    }
}
