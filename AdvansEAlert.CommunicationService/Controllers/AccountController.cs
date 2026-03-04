using System.Security.Claims;
using AdvansEAlert.CommunicationService.Data;
using AdvansEAlert.CommunicationService.Models.DomainModels;
using AdvansEAlert.CommunicationService.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdvansEAlert.CommunicationService.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) 
        {
            _logger.LogWarning("Invalid model state for login attempt");
            return View(model);
        }

        _logger.LogInformation("Login attempt for user: {Username}", model.Username);

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(rp => rp.RolePermissions)
                        .ThenInclude(p => p.Permission)
            .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

        if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            // Add roles and permissions as claims
            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                foreach (var rolePerm in userRole.Role.RolePermissions)
                {
                    claims.Add(new Claim("Permission", rolePerm.Permission.Name));
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            _logger.LogInformation("User {Username} logged in successfully.", user.Username);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Admin");
        }

        _logger.LogWarning("Invalid login attempt for user: {Username}", model.Username);
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User {Username} logged out.", User.Identity?.Name);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email)) return View();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user != null)
        {
            _logger.LogInformation("Password reset requested for email: {Email}", email);
            user.ResetToken = Guid.NewGuid().ToString("N");
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var resetLink = Url.Action("ResetPassword", "Account", new { token = user.ResetToken }, Request.Scheme);
            
            // In a real app, we'd use a template. For now, simple body.
            var body = $@"
                <h3>Password Reset Request</h3>
                <p>Hello {user.Username},</p>
                <p>We received a request to reset your password. Click the link below to proceed:</p>
                <p><a href='{resetLink}'>Reset My Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request this, please ignore this email.</p>
            ";

            // Using applicationId 1 (Admin) for system emails
            await _context.EmailLogs.AddAsync(new EmailLog
            {
                ApplicationId = 1,
                Recipient = email,
                Subject = "Password Reset Request",
                Body = body,
                Status = "Pending",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Since IEmailService is available, let's use it if possible, but the existing pattern seems to be logging.
            // Actually, IController has IEmailService in AdminController. Let's see if AccountController needs it.
        }
        else
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
        }

        TempData["Message"] = "If an account exists with that email, a reset link has been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token)) return RedirectToAction(nameof(Login));

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
        if (user == null)
        {
            TempData["Error"] = "Invalid or expired reset token.";
            return RedirectToAction(nameof(Login));
        }

        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match.");
            ViewBag.Token = token;
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
        if (user == null)
        {
            TempData["Error"] = "Invalid or expired reset token.";
            return RedirectToAction(nameof(Login));
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for user: {Username}", user.Username);

        TempData["Message"] = "Your password has been reset successfully. You can now login.";
        return RedirectToAction(nameof(Login));
    }
}
