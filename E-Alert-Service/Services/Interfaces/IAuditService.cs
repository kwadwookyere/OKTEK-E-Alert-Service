namespace EAlertService.Services.Interfaces;

// Bug fix #13: Moved from Services.Implementations namespace so it can be properly mocked
// in tests and follows the same pattern as IEmailService, ISmsService, etc.
public interface IAuditService
{
    Task LogAsync(string userId, string action, string entity, int? entityId, string details, string ipAddress);
}

