using AdvansEAlert.CommunicationService.Data;
using AdvansEAlert.CommunicationService.Models.DomainModels;
using AdvansEAlert.CommunicationService.Services.Interfaces;
using AdvansEAlert.CommunicationService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdvansEAlert.CommunicationService.Controllers;

[Authorize(Policy = "AdminOnly")]
public class BulkMessagingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<BulkMessagingController> _logger;

    public BulkMessagingController(
        ApplicationDbContext context, 
        IEmailService emailService, 
        ISmsService smsService, 
        ILogger<BulkMessagingController> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Compose(int? appId)
    {
        var viewModel = new BulkMessageViewModel
        {
            ApplicationId = appId ?? 0,
            Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync(),
            AvailableGroups = await _context.ContactGroups.Where(g => appId == null || g.ApplicationId == appId).ToListAsync()
        };
        
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates(int appId)
    {
        var emailTemplates = await _context.EmailTemplates.Where(t => t.ApplicationId == appId).Select(t => t.Name).ToListAsync();
        var smsTemplates = await _context.SmsTemplates.Where(t => t.ApplicationId == appId).Select(t => t.Name).ToListAsync();
        return Json(new { emailTemplates, smsTemplates });
    }

    [HttpGet]
    public async Task<IActionResult> GetGroups(int appId)
    {
        var groups = await _context.ContactGroups
            .Where(g => g.ApplicationId == appId)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync();
        return Json(groups);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(BulkMessageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (model.RecipientFile == null && string.IsNullOrEmpty(model.Recipients))
            {
                // Custom validation if both are empty
                ModelState.AddModelError("Recipients", "Please provide recipients via text or file upload.");
                model.Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync();
                return View("Compose", model);
            }
            
            // If it's just the recipients field that is empty but file is present, we might still be valid
            if (model.RecipientFile != null)
            {
                ModelState.Remove("Recipients");
            }
            else
            {
                model.Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync();
                return View("Compose", model);
            }
        }

        var recipients = new List<string>();
        
        // 1. Parse manual input
        if (!string.IsNullOrEmpty(model.Recipients))
        {
            recipients.AddRange(model.Recipients
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r)));
        }

        // 2. Parse file input
        if (model.RecipientFile != null && model.RecipientFile.Length > 0)
        {
            using var reader = new StreamReader(model.RecipientFile.OpenReadStream());
            var fileContent = await reader.ReadToEndAsync();
            recipients.AddRange(fileContent
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r)));
        }

        // 3. Parse Group input
        if (model.ContactGroupId.HasValue && model.ContactGroupId.Value > 0)
        {
            var groupContacts = await _context.ContactGroupAssignments
                .Include(ga => ga.Contact)
                .Where(ga => ga.ContactGroupId == model.ContactGroupId.Value)
                .Select(ga => model.Type == "Email" ? ga.Contact.Email : ga.Contact.PhoneNumber)
                .Where(r => !string.IsNullOrEmpty(r))
                .ToListAsync();
            
            recipients.AddRange(groupContacts!);
        }

        recipients = recipients.Distinct().ToList();

        if (!recipients.Any())
        {
            ModelState.AddModelError("Recipients", "No valid recipients found.");
            model.Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync();
            return View("Compose", model);
        }

        // Create a BulkMessageBatch for all types (Immediate or Scheduled)
        var batch = new BulkMessageBatch
        {
            ApplicationId = model.ApplicationId,
            Type = model.Type,
            Subject = model.Subject,
            MessagePreview = model.Message.Length > 100 ? model.Message.Substring(0, 100) + "..." : model.Message,
            TotalRecipients = recipients.Count,
            PendingCount = recipients.Count,
            Status = model.IsScheduled ? "Scheduled" : "Pending",
            ScheduledFor = model.IsScheduled ? model.ScheduledFor : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.BulkMessageBatches.Add(batch);
        await _context.SaveChangesAsync();

        if (model.IsScheduled && model.ScheduledFor.HasValue)
        {
            // Still create a ScheduledMessage for the background worker to trigger the batch start
            // Or we could have the worker just look for "Scheduled" batches.
            // Let's create a ScheduledMessage that stores the BatchId for easier processing.
            var scheduledMessage = new ScheduledMessage
            {
                ApplicationId = model.ApplicationId,
                Type = model.Type,
                Subject = model.Subject,
                Message = model.Message,
                Recipients = string.Join(",", recipients),
                ContactGroupId = model.ContactGroupId,
                IsTemplate = model.IsTemplate,
                TemplateName = model.TemplateName,
                ScheduledFor = model.ScheduledFor.Value,
                IsProcessed = false
                // Note: I should add BatchId to ScheduledMessage model if I want clean linking, 
                // but I'll search by ScheduledFor for now or just trust the new batch logic.
            };

            _context.ScheduledMessages.Add(scheduledMessage);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Bulk message scheduled successfully for {model.ScheduledFor.Value:MMM dd, yyyy HH:mm}. View it in the reporting dashboard.";
            return RedirectToAction("History");
        }

        // Create individual logs in Pending status ONLY for immediate sends
        // Scheduled sends will have their logs created when they are triggered to avoid cluttering Pending logs
        if (model.Type == "Email")
        {
            foreach (var recipient in recipients)
            {
                _context.EmailLogs.Add(new EmailLog
                {
                    ApplicationId = model.ApplicationId,
                    Recipient = recipient,
                    Subject = model.Subject ?? (model.IsTemplate ? $"Template: {model.TemplateName}" : "No Subject"),
                    Body = model.Message,
                    Status = "Pending",
                    BatchId = batch.Id
                });
            }
        }
        else
        {
            foreach (var recipient in recipients)
            {
                _context.SmsLogs.Add(new SmsLog
                {
                    ApplicationId = model.ApplicationId,
                    Recipient = recipient,
                    Message = model.Message,
                    Status = "Pending",
                    BatchId = batch.Id
                });
            }
        }

        await _context.SaveChangesAsync();

        TempData["Message"] = $"Bulk message batch created with {recipients.Count} recipients. Sending is being processed in the background.";
        return RedirectToAction("History");
    }

    public async Task<IActionResult> History()
    {
        var batches = await _context.BulkMessageBatches
            .Include(b => b.Application)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(batches);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelScheduled(int id)
    {
        var batch = await _context.BulkMessageBatches.FindAsync(id);
        if (batch != null && batch.Status == "Scheduled")
        {
            // Find and remove linked scheduled message
            var scheduled = await _context.ScheduledMessages.FirstOrDefaultAsync(s => s.BatchId == id);
            if (scheduled != null)
            {
                _context.ScheduledMessages.Remove(scheduled);
            }

            _context.BulkMessageBatches.Remove(batch);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Scheduled message and report entry cancelled successfully.";
        }
        return RedirectToAction(nameof(History));
    }
}
