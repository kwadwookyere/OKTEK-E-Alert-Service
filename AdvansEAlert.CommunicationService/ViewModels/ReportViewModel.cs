using AdvansEAlert.CommunicationService.Models.DomainModels;

namespace AdvansEAlert.CommunicationService.ViewModels;

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

public class CommunicationLogViewModel
{
    public List<EmailLog> EmailLogs { get; set; } = new();
    public List<SmsLog> SmsLogs { get; set; } = new();
    
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
