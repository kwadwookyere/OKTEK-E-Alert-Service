namespace AdvansEAlert.CommunicationService.ViewModels;

public class EmailRequest
{
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int? Priority { get; set; }
}

public class EmailTemplateRequest
{
    public string Recipient { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Dictionary<string, string> Placeholders { get; set; } = new();
    public int? Priority { get; set; }
}

public class SmsRequest
{
    public string Recipient { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? Priority { get; set; }
}

public class SmsTemplateRequest
{
    public string Recipient { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Dictionary<string, string> Placeholders { get; set; } = new();
    public int? Priority { get; set; }
}

public class UnifiedNotificationRequest
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EmailTemplateName { get; set; }
    public string? SmsTemplateName { get; set; }
    public Dictionary<string, string>? Placeholders { get; set; }
}
