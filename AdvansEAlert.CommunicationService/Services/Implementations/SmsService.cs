using AdvansEAlert.CommunicationService.Data;
using AdvansEAlert.CommunicationService.Infrastructure.Encryption;
using AdvansEAlert.CommunicationService.Models.DomainModels;
using AdvansEAlert.CommunicationService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace AdvansEAlert.CommunicationService.Services.Implementations;

public class SmsService : ISmsService
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsProviderResolver _smsProviderResolver;
    private readonly ITemplateEngine _templateEngine;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SmsService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public SmsService(
        ApplicationDbContext context,
        ISmsProviderResolver smsProviderResolver,
        ITemplateEngine templateEngine,
        IEncryptionService encryptionService,
        ILogger<SmsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _smsProviderResolver = smsProviderResolver;
        _templateEngine = templateEngine;
        _encryptionService = encryptionService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> SendSmsAsync(int applicationId, string recipient, string message, int? priority = null)
    {
        var config = await _smsProviderResolver.ResolveSmsProviderAsync(applicationId, priority);
        if (config == null)
        {
            _logger.LogError("No SMS provider configuration found for application {ApplicationId}", applicationId);
            return false;
        }

        var log = new SmsLog
        {
            ApplicationId = applicationId,
            Recipient = recipient,
            Message = message,
            UsedSmsProviderId = config.Id,
            Status = "Pending"
        };
        _context.SmsLogs.Add(log);
        await _context.SaveChangesAsync();

        bool success = false;
        try
        {
            if (config.ProviderName.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            {
                success = await SendViaTwilio(config, recipient, message);
            }
            else if (config.ProviderName.Equals("Hubtel", StringComparison.OrdinalIgnoreCase))
            {
                success = await SendViaHubtel(config, recipient, message);
            }
            else
            {
                _logger.LogError("Unsupported SMS provider: {ProviderName}", config.ProviderName);
                log.ErrorMessage = "Unsupported provider";
            }

            if (success)
            {
                log.Status = "Sent";
            }
            else
            {
                log.Status = "Failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Recipient}", recipient);
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
        }

        await _context.SaveChangesAsync();
        return success;
    }

    private async Task<bool> SendViaTwilio(SmsProviderConfiguration config, string recipient, string message)
    {
        var accountSid = _encryptionService.Decrypt(config.ApiKeyEncrypted);
        var authToken = _encryptionService.Decrypt(config.ApiSecretEncrypted ?? string.Empty);
        
        TwilioClient.Init(accountSid, authToken);

        var sms = await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(config.SenderId),
            to: new Twilio.Types.PhoneNumber(recipient)
        );

        return sms.Status != MessageResource.StatusEnum.Failed;
    }

    private async Task<bool> SendViaHubtel(SmsProviderConfiguration config, string recipient, string message)
    {
        // Hubtel REST API Implementation
        var client = _httpClientFactory.CreateClient();
        var clientId = _encryptionService.Decrypt(config.ApiKeyEncrypted);
        var clientSecret = _encryptionService.Decrypt(config.ApiSecretEncrypted ?? string.Empty);

        var url = $"https://smsc.hubtel.com/v1/messages/send?clientid={clientId}&clientsecret={clientSecret}&from={config.SenderId}&to={recipient}&content={Uri.EscapeDataString(message)}";
        
        var response = await client.GetAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SendSmsWithTemplateAsync(int applicationId, string recipient, string templateName, IDictionary<string, string> placeholders, int? priority = null)
    {
        var template = await _context.SmsTemplates
            .FirstOrDefaultAsync(t => t.ApplicationId == applicationId && t.Name == templateName);

        if (template == null)
        {
            _logger.LogError("SMS template {TemplateName} not found for application {ApplicationId}", templateName, applicationId);
            return false;
        }

        var renderedContent = _templateEngine.Render(template.Content, placeholders);

        return await SendSmsAsync(applicationId, recipient, renderedContent, priority);
    }
}
