using AdvansEAlert.CommunicationService.Data;
using AdvansEAlert.CommunicationService.Models.DomainModels;
using AdvansEAlert.CommunicationService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdvansEAlert.CommunicationService.Services.Implementations;

public class MessagingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessagingBackgroundService> _logger;

    public MessagingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<MessagingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Messaging Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledMessagesAsync(stoppingToken);
                await ProcessBulkBatchesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Messaging Background Service loop.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Messaging Background Service is stopping.");
    }

    private async Task ProcessScheduledMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingMessages = await context.ScheduledMessages
            .Include(m => m.Batch)
            .Where(m => !m.IsProcessed && m.ScheduledFor <= DateTime.UtcNow)
            .ToListAsync(stoppingToken);

        if (!pendingMessages.Any()) return;

        _logger.LogInformation("Found {Count} pending scheduled messages to activate.", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                if (message.Batch != null)
                {
                    // Activate the linked batch
                    message.Batch.Status = "Pending";
                    message.Batch.ScheduledFor = null; // No longer scheduled

                    // Create individual logs for the batch
                    var recipients = message.Recipients
                        .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim())
                        .ToList();

                    if (message.Type == "Email")
                    {
                        foreach (var recipient in recipients)
                        {
                            context.EmailLogs.Add(new EmailLog
                            {
                                ApplicationId = message.ApplicationId,
                                Recipient = recipient,
                                Subject = message.Subject ?? (message.IsTemplate ? $"Template: {message.TemplateName}" : "No Subject"),
                                Body = message.Message,
                                Status = "Pending",
                                BatchId = message.BatchId
                            });
                        }
                    }
                    else
                    {
                        foreach (var recipient in recipients)
                        {
                            context.SmsLogs.Add(new SmsLog
                            {
                                ApplicationId = message.ApplicationId,
                                Recipient = recipient,
                                Message = message.Message,
                                Status = "Pending",
                                BatchId = message.BatchId
                            });
                        }
                    }
                }

                message.IsProcessed = true;
                _logger.LogInformation("Activated batch reporting for scheduled message {Id}.", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate scheduled message {Id}.", message.Id);
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }

    private async Task ProcessBulkBatchesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

        // Find batches that are Pending or still Processing
        var activeBatches = await context.BulkMessageBatches
            .Where(b => b.Status == "Pending" || b.Status == "Processing")
            .ToListAsync(stoppingToken);

        foreach (var batch in activeBatches)
        {
            if (stoppingToken.IsCancellationRequested) break;

            if (batch.Status == "Pending")
            {
                batch.Status = "Processing";
                await context.SaveChangesAsync(stoppingToken);
            }

            if (batch.Type == "Email")
            {
                var emailLogs = await context.EmailLogs
                    .Where(l => l.BatchId == batch.Id && (l.Status == "Pending" || l.Status == "Retrying"))
                    .OrderBy(l => l.Id)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (!emailLogs.Any())
                {
                    // If no more pending/retrying logs, check if all are done
                    var unfinished = await context.EmailLogs.AnyAsync(l => l.BatchId == batch.Id && (l.Status == "Pending" || l.Status == "Retrying" || l.Status == "Processing"), stoppingToken);
                    if (!unfinished)
                    {
                        batch.Status = "Completed";
                    }
                }
                else
                {
                    foreach (var email in emailLogs)
                    {
                        // Check backoff for retrying messages
                        if (email.Status == "Retrying" && email.LastAttemptedAt.HasValue)
                        {
                            var backoffMinutes = Math.Pow(2, email.RetryCount) * 5; // 10, 20, 40 mins
                            if (DateTime.UtcNow < email.LastAttemptedAt.Value.AddMinutes(backoffMinutes))
                                continue;
                        }

                        email.Status = "Processing";
                        email.LastAttemptedAt = DateTime.UtcNow;
                        await context.SaveChangesAsync(stoppingToken);

                        try
                        {
                            var success = await emailService.SendEmailAsync(email.ApplicationId, email.Recipient, email.Subject, email.Body ?? "");
                            
                            if (success)
                            {
                                email.Status = "Sent";
                                email.ErrorMessage = null;
                                batch.SuccessCount++;
                                batch.PendingCount--;
                            }
                            else
                            {
                                HandleRetry(email, batch);
                            }
                        }
                        catch (Exception ex)
                        {
                            email.ErrorMessage = ex.Message;
                            HandleRetry(email, batch);
                        }

                        await context.SaveChangesAsync(stoppingToken);
                        await Task.Delay(50, stoppingToken); // Enterprise Throttling
                    }
                }
            }
            else
            {
                var smsLogs = await context.SmsLogs
                    .Where(l => l.BatchId == batch.Id && (l.Status == "Pending" || l.Status == "Retrying"))
                    .OrderBy(l => l.Id)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (!smsLogs.Any())
                {
                    var unfinished = await context.SmsLogs.AnyAsync(l => l.BatchId == batch.Id && (l.Status == "Pending" || l.Status == "Retrying" || l.Status == "Processing"), stoppingToken);
                    if (!unfinished)
                    {
                        batch.Status = "Completed";
                    }
                }
                else
                {
                    foreach (var sms in smsLogs)
                    {
                        if (sms.Status == "Retrying" && sms.LastAttemptedAt.HasValue)
                        {
                            var backoffMinutes = Math.Pow(2, sms.RetryCount) * 5;
                            if (DateTime.UtcNow < sms.LastAttemptedAt.Value.AddMinutes(backoffMinutes))
                                continue;
                        }

                        sms.Status = "Processing";
                        sms.LastAttemptedAt = DateTime.UtcNow;
                        await context.SaveChangesAsync(stoppingToken);

                        try
                        {
                            var success = await smsService.SendSmsAsync(sms.ApplicationId, sms.Recipient, sms.Message);
                            
                            if (success)
                            {
                                sms.Status = "Sent";
                                sms.ErrorMessage = null;
                                batch.SuccessCount++;
                                batch.PendingCount--;
                            }
                            else
                            {
                                HandleRetry(sms, batch);
                            }
                        }
                        catch (Exception ex)
                        {
                            sms.ErrorMessage = ex.Message;
                            HandleRetry(sms, batch);
                        }

                        await context.SaveChangesAsync(stoppingToken);
                        await Task.Delay(50, stoppingToken); // Enterprise Throttling
                    }
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }

    private void HandleRetry(EmailLog log, BulkMessageBatch batch)
    {
        log.RetryCount++;
        if (log.RetryCount < 3)
        {
            log.Status = "Retrying";
        }
        else
        {
            log.Status = "Failed";
            batch.FailureCount++;
            batch.PendingCount--;
        }
    }

    private void HandleRetry(SmsLog log, BulkMessageBatch batch)
    {
        log.RetryCount++;
        if (log.RetryCount < 3)
        {
            log.Status = "Retrying";
        }
        else
        {
            log.Status = "Failed";
            batch.FailureCount++;
            batch.PendingCount--;
        }
    }
}
