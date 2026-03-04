using AdvansEAlert.CommunicationService.Data;
using AdvansEAlert.CommunicationService.Infrastructure.Encryption;
using AdvansEAlert.CommunicationService.Models.DomainModels;
using AdvansEAlert.CommunicationService.Services.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace AdvansEAlert.CommunicationService.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ISmtpResolver _smtpResolver;
    private readonly ITemplateEngine _templateEngine;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        ApplicationDbContext context,
        ISmtpResolver smtpResolver,
        ITemplateEngine templateEngine,
        IEncryptionService encryptionService,
        ILogger<EmailService> logger)
    {
        _context = context;
        _smtpResolver = smtpResolver;
        _templateEngine = templateEngine;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(int applicationId, string recipient, string subject, string body, int? priority = null)
    {
        var config = await _smtpResolver.ResolveSmtpConfigurationAsync(applicationId, priority);
        if (config == null)
        {
            _logger.LogError("No SMTP configuration found for application {ApplicationId}", applicationId);
            return false;
        }

        var log = new EmailLog
        {
            ApplicationId = applicationId,
            Recipient = recipient,
            Subject = subject,
            Body = body.Length > 1000 ? body[..1000] + "..." : body, // Truncate log body
            UsedSmtpConfigurationId = config.Id,
            Status = "Pending"
        };
        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync();

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.FromName ?? config.FromEmail, config.FromEmail));
            message.To.Add(new MailboxAddress("", recipient));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(config.Host, config.Port, config.EnableSsl);
            
            var password = _encryptionService.Decrypt(config.PasswordEncrypted);
            await client.AuthenticateAsync(config.Username, password);
            
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            log.Status = "Sent";
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync();
            return false;
        }
    }

    public async Task<bool> SendEmailWithTemplateAsync(int applicationId, string recipient, string templateName, IDictionary<string, string> placeholders, int? priority = null)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.ApplicationId == applicationId && t.Name == templateName);

        if (template == null)
        {
            _logger.LogError("Email template {TemplateName} not found for application {ApplicationId}", templateName, applicationId);
            return false;
        }

        var renderedBody = _templateEngine.Render(template.BodyHtml, placeholders);
        var renderedSubject = _templateEngine.Render(template.Subject, placeholders);

        return await SendEmailAsync(applicationId, recipient, renderedSubject, renderedBody, priority);
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(SmtpConfiguration config, string password)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(config.Host, config.Port, config.EnableSsl);
            await client.AuthenticateAsync(config.Username, password);
            await client.DisconnectAsync(true);
            return (true, "Success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP test failed for {Host}", config.Host);
            return (false, ex.Message);
        }
    }
}

