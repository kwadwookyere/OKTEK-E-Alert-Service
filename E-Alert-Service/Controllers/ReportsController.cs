using EAlertService.Data;
using EAlertService.Models.DomainModels;
using EAlertService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace EAlertService.Controllers;

[Authorize(Policy = "AdminOnly")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new ReportsDashboardViewModel();

        // Summary Stats
        viewModel.TotalEmailSent = await _context.EmailLogs.CountAsync(l => l.Status == "Sent");
        viewModel.TotalEmailFailed = await _context.EmailLogs.CountAsync(l => l.Status == "Failed");
        viewModel.TotalSmsSent = await _context.SmsLogs.CountAsync(l => l.Status == "Sent");
        viewModel.TotalSmsFailed = await _context.SmsLogs.CountAsync(l => l.Status == "Failed");

        // Trends (Last 7 days)
        var startDate = DateTime.UtcNow.Date.AddDays(-6);
        
        var emailTrend = await _context.EmailLogs
            .Where(l => l.Timestamp >= startDate)
            .GroupBy(l => l.Timestamp.Date)
            .Select(g => new ChartDataPoint { Label = g.Key.ToString("MMM dd"), Value = g.Count() })
            .ToListAsync();

        var smsTrend = await _context.SmsLogs
            .Where(l => l.Timestamp >= startDate)
            .GroupBy(l => l.Timestamp.Date)
            .Select(g => new ChartDataPoint { Label = g.Key.ToString("MMM dd"), Value = g.Count() })
            .ToListAsync();

        // Fill missing dates with 0
        viewModel.EmailTrend = FillMissingDates(startDate, emailTrend);
        viewModel.SmsTrend = FillMissingDates(startDate, smsTrend);

        // Top Applications
        var emailStats = await _context.EmailLogs
            .GroupBy(l => l.Application.Name)
            .Select(g => new { Name = g.Key, Count = g.Count(), Sent = g.Count(x => x.Status == "Sent") })
            .ToListAsync();

        var smsStats = await _context.SmsLogs
            .GroupBy(l => l.Application.Name)
            .Select(g => new { Name = g.Key, Count = g.Count(), Sent = g.Count(x => x.Status == "Sent") })
            .ToListAsync();

        var allApps = emailStats.Select(s => s.Name).Union(smsStats.Select(s => s.Name)).Distinct();

        foreach (var appName in allApps)
        {
            var eStat = emailStats.FirstOrDefault(s => s.Name == appName);
            var sStat = smsStats.FirstOrDefault(s => s.Name == appName);

            int total = (eStat?.Count ?? 0) + (sStat?.Count ?? 0);
            int sent = (eStat?.Sent ?? 0) + (sStat?.Sent ?? 0);

            viewModel.TopApplications.Add(new AppStats
            {
                AppName = appName,
                TotalCount = total,
                SuccessRate = total > 0 ? (double)sent / total * 100 : 0
            });
        }

        viewModel.TopApplications = viewModel.TopApplications.OrderByDescending(a => a.TotalCount).Take(5).ToList();

        return View(viewModel);
    }

    public async Task<IActionResult> CommunicationLog(
        string? app, 
        string? type, 
        string? status, 
        DateTime? start, 
        DateTime? end, 
        int page = 1, 
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 5) pageSize = 20;

        var apps = await _context.Applications.ToListAsync();
        ViewBag.Applications = apps;

        var viewModel = new CommunicationLogViewModel
        {
            SelectedApp = app,
            SelectedType = type,
            SelectedStatus = status,
            StartDate = start,
            EndDate = end,
            PageNumber = page,
            PageSize = pageSize,
            Applications = apps.Select(a => a.Name).ToList()
        };

        var emailQuery = _context.EmailLogs.Include(l => l.Application).AsQueryable();
        var smsQuery = _context.SmsLogs.Include(l => l.Application).AsQueryable();

        if (!string.IsNullOrEmpty(app))
        {
            emailQuery = emailQuery.Where(l => l.Application.Name == app);
            smsQuery = smsQuery.Where(l => l.Application.Name == app);
        }

        if (!string.IsNullOrEmpty(status))
        {
            emailQuery = emailQuery.Where(l => l.Status == status);
            smsQuery = smsQuery.Where(l => l.Status == status);
        }

        if (start.HasValue)
        {
            emailQuery = emailQuery.Where(l => l.Timestamp >= start.Value);
            smsQuery = smsQuery.Where(l => l.Timestamp >= start.Value);
        }

        if (end.HasValue)
        {
            var endInclusive = end.Value.Date.AddDays(1);
            emailQuery = emailQuery.Where(l => l.Timestamp < endInclusive);
            smsQuery = smsQuery.Where(l => l.Timestamp < endInclusive);
        }

        if (type == "Email")
        {
            viewModel.TotalRecords = await emailQuery.CountAsync();
            var emailItems = await emailQuery
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            viewModel.EmailLogs = emailItems;
            viewModel.Items = emailItems.Select(e => new CommunicationLogItem
            {
                Id = e.Id,
                Timestamp = e.Timestamp,
                Type = "Email",
                ApplicationName = e.Application?.Name ?? "Unknown",
                Recipient = e.Recipient,
                SubjectOrMessage = e.Subject ?? "",
                Status = e.Status,
                ErrorMessage = e.ErrorMessage
            }).ToList();
        }
        else if (type == "SMS")
        {
            viewModel.TotalRecords = await smsQuery.CountAsync();
            var smsItems = await smsQuery
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            viewModel.SmsLogs = smsItems;
            viewModel.Items = smsItems.Select(s => new CommunicationLogItem
            {
                Id = s.Id,
                Timestamp = s.Timestamp,
                Type = "SMS",
                ApplicationName = s.Application?.Name ?? "Unknown",
                Recipient = s.Recipient,
                SubjectOrMessage = s.Message ?? "",
                Status = s.Status,
                ErrorMessage = s.ErrorMessage
            }).ToList();
        }
        else
        {
            var emailCount = await emailQuery.CountAsync();
            var smsCount = await smsQuery.CountAsync();
            viewModel.TotalRecords = emailCount + smsCount;

            var emailProjected = emailQuery.Select(e => new CommunicationLogItem
            {
                Id = e.Id,
                Timestamp = e.Timestamp,
                Type = "Email",
                ApplicationName = e.Application.Name,
                Recipient = e.Recipient,
                SubjectOrMessage = e.Subject,
                Status = e.Status,
                ErrorMessage = e.ErrorMessage
            });

            var smsProjected = smsQuery.Select(s => new CommunicationLogItem
            {
                Id = s.Id,
                Timestamp = s.Timestamp,
                Type = "SMS",
                ApplicationName = s.Application.Name,
                Recipient = s.Recipient,
                SubjectOrMessage = s.Message,
                Status = s.Status,
                ErrorMessage = s.ErrorMessage
            });

            viewModel.Items = await emailProjected
                .Concat(smsProjected)
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        return View(viewModel);
    }

    public async Task<IActionResult> ExportCsv(string? app, string? type, string? status, DateTime? start, DateTime? end)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Type,Application,Recipient,Subject/Message,Status,Error");

        if (string.IsNullOrEmpty(type) || type == "Email")
        {
            var emailQuery = _context.EmailLogs.Include(l => l.Application).AsQueryable();
            if (!string.IsNullOrEmpty(app)) emailQuery = emailQuery.Where(l => l.Application.Name == app);
            if (!string.IsNullOrEmpty(status)) emailQuery = emailQuery.Where(l => l.Status == status);
            if (start.HasValue) emailQuery = emailQuery.Where(l => l.Timestamp >= start.Value);
            if (end.HasValue) emailQuery = emailQuery.Where(l => l.Timestamp <= end.Value.AddDays(1));

            var logs = await emailQuery.OrderByDescending(l => l.Timestamp).ToListAsync();
            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Timestamp:G},Email,{EscapeCsv(log.Application.Name)},{EscapeCsv(log.Recipient)},{EscapeCsv(log.Subject)},{log.Status},{EscapeCsv(log.ErrorMessage)}");
            }
        }

        if (string.IsNullOrEmpty(type) || type == "SMS")
        {
            var smsQuery = _context.SmsLogs.Include(l => l.Application).AsQueryable();
            if (!string.IsNullOrEmpty(app)) smsQuery = smsQuery.Where(l => l.Application.Name == app);
            if (!string.IsNullOrEmpty(status)) smsQuery = smsQuery.Where(l => l.Status == status);
            if (start.HasValue) smsQuery = smsQuery.Where(l => l.Timestamp >= start.Value);
            if (end.HasValue) smsQuery = smsQuery.Where(l => l.Timestamp <= end.Value.AddDays(1));

            var logs = await smsQuery.OrderByDescending(l => l.Timestamp).ToListAsync();
            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Timestamp:G},SMS,{EscapeCsv(log.Application.Name)},{EscapeCsv(log.Recipient)},{EscapeCsv(log.Message)},{log.Status},{EscapeCsv(log.ErrorMessage)}");
            }
        }

        var fileName = $"CommunicationReport_{DateTime.Now:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    private List<ChartDataPoint> FillMissingDates(DateTime start, List<ChartDataPoint> existing)
    {
        var result = new List<ChartDataPoint>();
        for (int i = 0; i < 7; i++)
        {
            var date = start.AddDays(i);
            var label = date.ToString("MMM dd");
            var found = existing.FirstOrDefault(e => e.Label == label);
            result.Add(new ChartDataPoint { Label = label, Value = found?.Value ?? 0 });
        }
        return result;
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
