using EAlertService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EAlertService.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private const string ApiKeyName = "X-API-KEY";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger, IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyName, out var extractedApiKey) ||
            string.IsNullOrWhiteSpace(extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key was not provided.");
            return;
        }

        string rawKey = extractedApiKey.ToString();

        // Bug fix #2: Check cache first to avoid BCrypt on every request
        if (_cache.TryGetValue<int>(rawKey, out var cachedAppId))
        {
            context.Items["ApplicationId"] = cachedAppId;
            await _next(context);
            return;
        }

        // Load only active apps and verify — BCrypt is slow by design; cache the result
        var apps = await dbContext.Applications
            .Where(a => a.IsEnabled)
            .ToListAsync();

        int? authorizedAppId = null;
        foreach (var app in apps)
        {
            if (BCrypt.Net.BCrypt.Verify(rawKey, app.ApiKeyHash))
            {
                authorizedAppId = app.Id;
                break;
            }
        }

        if (authorizedAppId is null)
        {
            _logger.LogWarning("Unauthorized API access attempt. IP: {RemoteIpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized client.");
            return;
        }

        // Cache the verified mapping so subsequent requests skip BCrypt
        _cache.Set(rawKey, authorizedAppId.Value, CacheDuration);

        context.Items["ApplicationId"] = authorizedAppId.Value;
        await _next(context);
    }
}
