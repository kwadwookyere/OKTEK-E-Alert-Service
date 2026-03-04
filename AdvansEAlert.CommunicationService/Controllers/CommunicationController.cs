using AdvansEAlert.CommunicationService.Services.Interfaces;
using AdvansEAlert.CommunicationService.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdvansEAlert.CommunicationService.Controllers;

[ApiController]
[Route("api/communication")]
public class CommunicationController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;

    public CommunicationController(IEmailService emailService, ISmsService smsService)
    {
        _emailService = emailService;
        _smsService = smsService;
    }

    private int ApplicationId => (int)HttpContext.Items["ApplicationId"]!;

    [HttpPost("notify")]
    public async Task<IActionResult> Notify([FromBody] UnifiedNotificationRequest request)
    {
        bool emailSuccess = false;
        
        // Try Email first
        if (!string.IsNullOrEmpty(request.RecipientEmail))
        {
            if (!string.IsNullOrEmpty(request.EmailTemplateName))
            {
                emailSuccess = await _emailService.SendEmailWithTemplateAsync(
                    ApplicationId, request.RecipientEmail, request.EmailTemplateName, request.Placeholders ?? new());
            }
            else
            {
                emailSuccess = await _emailService.SendEmailAsync(
                    ApplicationId, request.RecipientEmail, request.Subject, request.Message);
            }
        }

        if (emailSuccess)
        {
            return Ok(new { Message = "Notification sent via Email", Channel = "Email" });
        }

        // If email fails or not provided -> send SMS
        if (!string.IsNullOrEmpty(request.RecipientPhone))
        {
            bool smsSuccess = false;
            if (!string.IsNullOrEmpty(request.SmsTemplateName))
            {
                smsSuccess = await _smsService.SendSmsWithTemplateAsync(
                    ApplicationId, request.RecipientPhone, request.SmsTemplateName, request.Placeholders ?? new());
            }
            else
            {
                smsSuccess = await _smsService.SendSmsAsync(
                    ApplicationId, request.RecipientPhone, request.Message);
            }

            if (smsSuccess)
            {
                return Ok(new { Message = "Notification sent via SMS", Channel = "SMS" });
            }
        }

        return BadRequest("Failed to send notification via any channel");
    }
}
