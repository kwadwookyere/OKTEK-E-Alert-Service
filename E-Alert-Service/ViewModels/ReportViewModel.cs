using EAlertService.Models.DomainModels;

namespace EAlertService.ViewModels;

public class ReportsDashboardViewModel
{
    public int TotalEmailSent { get; set; }
    public int TotalEmailFailed { get; set; }
    public int TotalSmsSent { get; set; }
    public int TotalSmsFailed { get; set; }
    
    public List<ChartDataPoint> EmailTrend { get; set; } = new();
    public List<ChartDataPoint> SmsTrend { get; set; } = new();
    
    public List<AppStats> TopApplications { get; set; } = new();
}

public class CommunicationLogItem
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "Email"; // "Email" or "SMS"
    public string ApplicationName { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string SubjectOrMessage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class CommunicationLogViewModel : IPagedResult
{
    public List<CommunicationLogItem> Items { get; set; } = new();

    public List<EmailLog> EmailLogs { get; set; } = new();
    public List<SmsLog> SmsLogs { get; set; } = new();
    
    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalRecords { get; set; }

    public int TotalPages => TotalRecords > 0 ? (int)Math.Ceiling((double)TotalRecords / Math.Max(PageSize, 1)) : 1;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public int StartRecord => TotalRecords == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
    public int EndRecord => Math.Min(PageNumber * PageSize, TotalRecords);

    // Filters
    public string? SelectedApp { get; set; }
    public string? SelectedType { get; set; } // Email, SMS
    public string? SelectedStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public List<string> Applications { get; set; } = new();
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class AppStats
{
    public string AppName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public double SuccessRate { get; set; }
}
