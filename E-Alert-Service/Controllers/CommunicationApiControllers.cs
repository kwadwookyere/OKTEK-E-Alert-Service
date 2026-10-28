using EAlertService.Services.Interfaces;
using EAlertService.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EAlertService.Controllers;

[ApiController]
[Route("api/communication/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    // Bug fix #3: Safe ApplicationId — throws 500 with a clear message instead of
    // a cryptic NullReferenceException if middleware did not set the context item.
    private int ApplicationId
    {
        get
        {
            if (HttpContext.Items.TryGetValue("ApplicationId", out var id) && id is int appId)
                return appId;
            throw new InvalidOperationException("ApplicationId was not set by API key middleware. Ensure the request passed API key authentication.");
        }
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendTemplate([FromBody] EmailTemplateRequest request)
    {
        var success = await _emailService.SendEmailWithTemplateAsync(
            ApplicationId, request.Recipient, request.TemplateName, request.Placeholders, request.Priority);
        
        if (success)
        {
            _logger.LogInformation("Successfully sent email template {TemplateName} to {Recipient}", request.TemplateName, request.Recipient);
            return Ok(new { Message = "Email sent successfully" });
        }

        _logger.LogWarning("Failed to send email template {TemplateName} to {Recipient}", request.TemplateName, request.Recipient);
        return BadRequest("Failed to send email");
    }

    [HttpPost("send-raw")]
    public async Task<IActionResult> SendRaw([FromBody] EmailRequest request)
    {
        var success = await _emailService.SendEmailAsync(
            ApplicationId, request.Recipient, request.Subject, request.Body, request.Priority);

        if (success)
        {
            _logger.LogInformation("Successfully sent raw email to {Recipient}", request.Recipient);
            return Ok(new { Message = "Email sent successfully" });
        }

        _logger.LogWarning("Failed to send raw email to {Recipient}", request.Recipient);
        return BadRequest("Failed to send email");
    }
}

[ApiController]
[Route("api/communication/[controller]")]
public class SmsController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly ILogger<SmsController> _logger;

    public SmsController(ISmsService smsService, ILogger<SmsController> logger)
    {
        _smsService = smsService;
        _logger = logger;
    }

    // Bug fix #3: Safe ApplicationId — throws 500 with a clear message instead of
    // a cryptic NullReferenceException if middleware did not set the context item.
    private int ApplicationId
    {
        get
        {
            if (HttpContext.Items.TryGetValue("ApplicationId", out var id) && id is int appId)
                return appId;
            throw new InvalidOperationException("ApplicationId was not set by API key middleware. Ensure the request passed API key authentication.");
        }
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendTemplate([FromBody] SmsTemplateRequest request)
    {
        var success = await _smsService.SendSmsWithTemplateAsync(
            ApplicationId, request.Recipient, request.TemplateName, request.Placeholders, request.Priority);

        if (success)
        {
            _logger.LogInformation("Successfully sent SMS template {TemplateName} to {Recipient}", request.TemplateName, request.Recipient);
            return Ok(new { Message = "SMS sent successfully" });
        }

        _logger.LogWarning("Failed to send SMS template {TemplateName} to {Recipient}", request.TemplateName, request.Recipient);
        return BadRequest("Failed to send SMS");
    }

    [HttpPost("send-raw")]
    public async Task<IActionResult> SendRaw([FromBody] SmsRequest request)
    {
        var success = await _smsService.SendSmsAsync(
            ApplicationId, request.Recipient, request.Message, request.Priority);

        if (success)
        {
            _logger.LogInformation("Successfully sent raw SMS to {Recipient}", request.Recipient);
            return Ok(new { Message = "SMS sent successfully" });
        }

        _logger.LogWarning("Failed to send raw SMS to {Recipient}", request.Recipient);
        return BadRequest("Failed to send SMS");
    }
}
