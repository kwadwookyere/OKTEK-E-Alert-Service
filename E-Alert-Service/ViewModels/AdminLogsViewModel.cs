using System.Collections.Generic;
using EAlertService.Models.DomainModels;

namespace EAlertService.ViewModels;

public class AdminLogsViewModel
{
    public PagedResult<EmailLog> EmailLogs { get; set; } = new();
    public PagedResult<SmsLog> SmsLogs { get; set; } = new();
    public int? BatchId { get; set; }
    public string ActiveTab { get; set; } = "email";
}

