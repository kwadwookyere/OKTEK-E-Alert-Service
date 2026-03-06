using EAlertService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAlertService.Controllers;

[Route("t")]
public class TrackingController : Controller
{
    private readonly ApplicationDbContext _context;

    public TrackingController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("o/{id}")]
    public async Task<IActionResult> Open(Guid id)
    {
        var log = await _context.EmailLogs.FirstOrDefaultAsync(l => l.TrackingGuid == id);
        if (log != null && log.OpenedAt == null)
        {
            log.OpenedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Return a 1x1 transparent GIF
        byte[] pixel = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
        return File(pixel, "image/gif");
    }

    [HttpGet("c/{id}")]
    public async Task<IActionResult> Click(Guid id, [FromQuery] string url)
    {
        var log = await _context.EmailLogs.FirstOrDefaultAsync(l => l.TrackingGuid == id);
        if (log != null)
        {
            log.ClickedAt = DateTime.UtcNow;
            log.ClickCount++;
            await _context.SaveChangesAsync();
        }

        if (string.IsNullOrEmpty(url)) return Redirect("/");
        
        return Redirect(url);
    }
}
