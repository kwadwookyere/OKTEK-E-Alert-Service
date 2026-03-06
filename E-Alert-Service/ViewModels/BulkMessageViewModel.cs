using System.ComponentModel.DataAnnotations;
using EAlertService.Models.DomainModels;

namespace EAlertService.ViewModels;

public class BulkMessageViewModel
{
    [Required]
    public int ApplicationId { get; set; }

    [Required]
    public string Type { get; set; } = "Email"; // Email or SMS

    [Required]
    public string Recipients { get; set; } = string.Empty; // Comma, semicolon or newline separated

    public IFormFile? RecipientFile { get; set; }

    public bool IsTemplate { get; set; }
    public string? TemplateName { get; set; }

    public bool IsScheduled { get; set; }
    public DateTime? ScheduledFor { get; set; }

    public int? ContactGroupId { get; set; }
    public List<ContactGroup>? AvailableGroups { get; set; }

    public string? Subject { get; set; } // Required for Email if not a template

    [Required]
    public string Message { get; set; } = string.Empty;

    public List<Application>? Applications { get; set; }
}

public class BulkMessageResultViewModel
{
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public List<string> Failures { get; set; } = new List<string>();
}
