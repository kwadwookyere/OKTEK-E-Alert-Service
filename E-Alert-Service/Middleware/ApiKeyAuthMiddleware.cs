using EAlertService.Data;
using Microsoft.EntityFrameworkCore;

namespace EAlertService.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private const string APIKEYNAME = "X-API-KEY";

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key was not provided.");
            return;
        }

        // In a production app, we would hash the incoming key and compare with ApiKeyHash
        // For simplicity here, we assume the DB contains the clear text or we use BCrypt
        var app = await dbContext.Applications
            .FirstOrDefaultAsync(a => a.IsEnabled); // Need a better way to find the app from the key

        // Implementation Note: In a real scenario, you'd iterate or use a cache to find the app by key
        // Let's assume we have a way to verify the key. For now, we'll check all active apps.
        bool authorized = false;
        var apps = await dbContext.Applications.Where(a => a.IsEnabled).ToListAsync();
        foreach (var activeApp in apps)
        {
            if (BCrypt.Net.BCrypt.Verify(extractedApiKey, activeApp.ApiKeyHash))
            {
                context.Items["ApplicationId"] = activeApp.Id;
                authorized = true;
                break;
            }
        }

        if (!authorized)
        {
            _logger.LogWarning("Unauthorized client Attempt. IP: {RemoteIpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized client.");
            return;
        }

        await _next(context);
    }
}
