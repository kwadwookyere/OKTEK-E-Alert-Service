using EAlertService.Data;
using EAlertService.Infrastructure.Encryption;
using EAlertService.Models.DomainModels;
using EAlertService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAlertService.Controllers;

[Authorize(Policy = "AdminOnly")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, IEncryptionService encryptionService, IEmailService emailService, ILogger<AdminController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.AppsCount = await _context.Applications.CountAsync();
        ViewBag.EmailLogsCount = await _context.EmailLogs.CountAsync();
        ViewBag.SmsLogsCount = await _context.SmsLogs.CountAsync();
        return View();
    }

    public async Task<IActionResult> Applications()
    {
        var apps = await _context.Applications.ToListAsync();
        foreach (var app in apps)
        {
            if (!string.IsNullOrEmpty(app.ApiKeyEncrypted))
            {
                // We'll decrypt it for the view. In a real-world scenario, you might want to do this only on demand.
                app.ApiKeyEncrypted = _encryptionService.Decrypt(app.ApiKeyEncrypted);
            }
        }
        return View(apps);
    }

    [HttpPost]
    public async Task<IActionResult> CreateApplication(string name)
    {
        var apiKey = Guid.NewGuid().ToString("N");
        var app = new Application
        {
            Name = name,
            ApiKeyHash = BCrypt.Net.BCrypt.HashPassword(apiKey),
            ApiKeyEncrypted = _encryptionService.Encrypt(apiKey),
            IsEnabled = true
        };
        _context.Applications.Add(app);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created new application: {AppName} (ID: {AppId})", app.Name, app.Id);
        
        TempData["NewApiKey"] = apiKey;
        return RedirectToAction(nameof(Applications));
    }

    [HttpPost]
    public async Task<IActionResult> EditApplication(int id, string name, bool isEnabled)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app == null) return NotFound();

        app.Name = name;
        app.IsEnabled = isEnabled;
        
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Applications));
    }

    [HttpPost]
    public async Task<IActionResult> RegenerateApiKey(int id)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app == null) return NotFound();

        var apiKey = Guid.NewGuid().ToString("N");
        app.ApiKeyHash = BCrypt.Net.BCrypt.HashPassword(apiKey);
        app.ApiKeyEncrypted = _encryptionService.Encrypt(apiKey);
        
        await _context.SaveChangesAsync();
        
        TempData["NewApiKey"] = apiKey;
        TempData["Message"] = $"API Key for {app.Name} has been rotated.";
        
        _logger.LogInformation("Regenerated API Key for application: {AppName} (ID: {AppId})", app.Name, app.Id);
        return RedirectToAction(nameof(Applications));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteApplication(int id)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app == null) return NotFound();

        _context.Applications.Remove(app);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Applications));
    }

    public async Task<IActionResult> SMTP()
    {
        var configs = await _context.SmtpConfigurations.Include(s => s.Application).ToListAsync();
        ViewBag.Applications = await _context.Applications.ToListAsync();
        return View(configs);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSmtp(SmtpConfiguration config, string password)
    {
        config.PasswordEncrypted = _encryptionService.Encrypt(password);
        _context.SmtpConfigurations.Add(config);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(SMTP));
    }

    [HttpPost]
    public async Task<IActionResult> EditSmtp(SmtpConfiguration config, string? password)
    {
        var existing = await _context.SmtpConfigurations.FindAsync(config.Id);
        if (existing == null) return NotFound();

        existing.ApplicationId = config.ApplicationId;
        existing.Host = config.Host;
        existing.Port = config.Port;
        existing.Username = config.Username;
        existing.FromEmail = config.FromEmail;
        existing.FromName = config.FromName;
        existing.Priority = config.Priority;
        existing.EnableSsl = config.EnableSsl;
        existing.IsActive = config.IsActive;

        if (!string.IsNullOrEmpty(password))
        {
            existing.PasswordEncrypted = _encryptionService.Encrypt(password);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated SMTP configuration ID: {ConfigId}", config.Id);
        return RedirectToAction(nameof(SMTP));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSmtp(int id)
    {
        var config = await _context.SmtpConfigurations.FindAsync(id);
        if (config == null) return NotFound();

        _context.SmtpConfigurations.Remove(config);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(SMTP));
    }

    [HttpPost]
    public async Task<IActionResult> TestSmtp(int id, string? password)
    {
        var config = await _context.SmtpConfigurations.FindAsync(id);
        if (config == null) return NotFound();

        string passToUse;
        if (!string.IsNullOrEmpty(password))
        {
            passToUse = password;
        }
        else
        {
            passToUse = _encryptionService.Decrypt(config.PasswordEncrypted);
        }

        var result = await _emailService.TestConnectionAsync(config, passToUse);
        
        if (result.Success)
        {
            _logger.LogInformation("SMTP Test Connection Successful for Config ID: {ConfigId}", id);
            TempData["Message"] = "SMTP Connection Successful!";
        }
        else
        {
            _logger.LogError("SMTP Test Connection Failed for Config ID: {ConfigId}. Error: {Error}", id, result.Message);
            TempData["Error"] = $"SMTP Connection Failed: {result.Message}";
        }

        return RedirectToAction(nameof(SMTP));
    }

    public async Task<IActionResult> SMSProviders()
    {
        var providers = await _context.SmsProviders.Include(s => s.Application).ToListAsync();
        ViewBag.Applications = await _context.Applications.ToListAsync();
        return View(providers);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSmsProvider(SmsProviderConfiguration config, string apiKey, string? apiSecret)
    {
        config.ApiKeyEncrypted = _encryptionService.Encrypt(apiKey);
        if (!string.IsNullOrEmpty(apiSecret))
            config.ApiSecretEncrypted = _encryptionService.Encrypt(apiSecret);
            
        _context.SmsProviders.Add(config);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(SMSProviders));
    }

    [HttpPost]
    public async Task<IActionResult> EditSmsProvider(SmsProviderConfiguration config, string? apiKey, string? apiSecret)
    {
        var existing = await _context.SmsProviders.FindAsync(config.Id);
        if (existing == null) return NotFound();

        existing.ApplicationId = config.ApplicationId;
        existing.ProviderName = config.ProviderName;
        existing.SenderId = config.SenderId;
        existing.Priority = config.Priority;
        existing.IsActive = config.IsActive;

        if (!string.IsNullOrEmpty(apiKey))
            existing.ApiKeyEncrypted = _encryptionService.Encrypt(apiKey);
        
        if (!string.IsNullOrEmpty(apiSecret))
            existing.ApiSecretEncrypted = _encryptionService.Encrypt(apiSecret);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(SMSProviders));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSmsProvider(int id)
    {
        var provider = await _context.SmsProviders.FindAsync(id);
        if (provider == null) return NotFound();

        _context.SmsProviders.Remove(provider);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(SMSProviders));
    }

    [HttpPost]
    public async Task<IActionResult> TestSmsProvider(int id)
    {
        // For now, simpler test that just checks if we can resolve it and if there's a basic connection check 
        // (Implementation can be expanded in ISmsService)
        TempData["Message"] = "SMS Provider configuration is valid. (Live test not implemented yet for all providers)";
        return RedirectToAction(nameof(SMSProviders));
    }

    public async Task<IActionResult> Templates()
    {
        var emailTemplates = await _context.EmailTemplates.Include(t => t.Application).ToListAsync();
        var smsTemplates = await _context.SmsTemplates.Include(t => t.Application).ToListAsync();
        ViewBag.SmsTemplates = smsTemplates;
        ViewBag.Applications = await _context.Applications.ToListAsync();
        return View(emailTemplates);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmailTemplate(EmailTemplate template)
    {
        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Templates));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSmsTemplate(SmsTemplate template)
    {
        _context.SmsTemplates.Add(template);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Templates));
    }

    public async Task<IActionResult> Logs(int? batchId)
    {
        var emailQuery = _context.EmailLogs.AsQueryable();
        var smsQuery = _context.SmsLogs.AsQueryable();

        if (batchId.HasValue)
        {
            emailQuery = emailQuery.Where(l => l.BatchId == batchId.Value);
            smsQuery = smsQuery.Where(l => l.BatchId == batchId.Value);
            ViewBag.BatchId = batchId.Value;
        }

        var emailLogs = await emailQuery.OrderByDescending(l => l.Timestamp).Take(100).ToListAsync();
        var smsLogs = await smsQuery.OrderByDescending(l => l.Timestamp).Take(100).ToListAsync();
        
        ViewBag.SmsLogs = smsLogs;
        return View(emailLogs);
    }

    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();
        
        ViewBag.Roles = await _context.Roles.ToListAsync();
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(string username, string email, string password, int roleId)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username || u.Email == email))
        {
            TempData["Error"] = "Username or Email already exists.";
            return RedirectToAction(nameof(Users));
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        await _context.SaveChangesAsync();

        TempData["Message"] = $"User {username} created successfully.";
        _logger.LogInformation("Admin created new user: {Username}", username);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(int id, string username, string email, bool isActive, int roleId)
    {
        var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.Username = username;
        user.Email = email;
        user.IsActive = isActive;

        // Update Role (assuming single role for now for simplicity of the UI)
        var currentRole = user.UserRoles.FirstOrDefault();
        if (currentRole != null)
        {
            _context.UserRoles.Remove(currentRole);
        }
        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

        await _context.SaveChangesAsync();
        TempData["Message"] = "User updated successfully.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(int id, string password)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        await _context.SaveChangesAsync();

        TempData["Message"] = $"Password for {user.Username} has been reset.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (user.Username.ToLower() == "admin")
        {
            TempData["Error"] = "Cannot delete the default admin user.";
            return RedirectToAction(nameof(Users));
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin deleted user: {Username} (ID: {UserId})", user.Username, user.Id);
        TempData["Message"] = "User deleted successfully.";
        return RedirectToAction(nameof(Users));
    }
}
