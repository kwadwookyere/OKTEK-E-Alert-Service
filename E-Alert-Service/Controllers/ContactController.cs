using EAlertService.Data;
using EAlertService.Models.DomainModels;
using EAlertService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAlertService.Controllers;

[Authorize]
public class ContactController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly Services.Implementations.IAuditService _auditService;

    public ContactController(ApplicationDbContext context, Services.Implementations.IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new ContactListViewModel
        {
            Contacts = await _context.Contacts.Include(c => c.Application).Include(c => c.GroupAssignments).ThenInclude(ga => ga.ContactGroup).ToListAsync(),
            Groups = await _context.ContactGroups.Include(g => g.Application).ToListAsync(),
            Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync()
        };
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> CreateContact()
    {
        var viewModel = new ContactEditViewModel
        {
            Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync(),
            AllGroups = await _context.ContactGroups.ToListAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContact(ContactEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var contact = new Contact
            {
                ApplicationId = model.ApplicationId,
                Name = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            if (model.SelectedGroupIds != null && model.SelectedGroupIds.Any())
            {
                foreach (var groupId in model.SelectedGroupIds)
                {
                    _context.ContactGroupAssignments.Add(new ContactGroupAssignment
                    {
                        ContactId = contact.Id,
                        ContactGroupId = groupId
                    });
                }
                await _context.SaveChangesAsync();
            }

            await _auditService.LogAsync(User.Identity?.Name ?? "Unknown", "CreateContact", "Contact", contact.Id, $"Created contact: {contact.Name} ({contact.Email})", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            return RedirectToAction(nameof(Index));
        }

        model.Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync();
        model.AllGroups = await _context.ContactGroups.ToListAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditContact(int id)
    {
        var contact = await _context.Contacts.Include(c => c.GroupAssignments).FirstOrDefaultAsync(c => c.Id == id);
        if (contact == null) return NotFound();

        var viewModel = new ContactEditViewModel
        {
            Id = contact.Id,
            ApplicationId = contact.ApplicationId,
            Name = contact.Name,
            Email = contact.Email,
            PhoneNumber = contact.PhoneNumber,
            SelectedGroupIds = contact.GroupAssignments.Select(ga => ga.ContactGroupId).ToList(),
            Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync(),
            AllGroups = await _context.ContactGroups.Where(g => g.ApplicationId == contact.ApplicationId).ToListAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditContact(ContactEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var contact = await _context.Contacts.Include(c => c.GroupAssignments).FirstOrDefaultAsync(c => c.Id == model.Id);
            if (contact == null) return NotFound();

            contact.ApplicationId = model.ApplicationId;
            contact.Name = model.Name;
            contact.Email = model.Email;
            contact.PhoneNumber = model.PhoneNumber;

            // Update Groups
            var existingAssignments = contact.GroupAssignments.ToList();
            _context.ContactGroupAssignments.RemoveRange(existingAssignments);

            if (model.SelectedGroupIds != null)
            {
                foreach (var groupId in model.SelectedGroupIds)
                {
                    _context.ContactGroupAssignments.Add(new ContactGroupAssignment
                    {
                        ContactId = contact.Id,
                        ContactGroupId = groupId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        model.Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync();
        model.AllGroups = await _context.ContactGroups.Where(g => g.ApplicationId == model.ApplicationId).ToListAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContact(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact != null)
        {
            var name = contact.Name;
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(User.Identity?.Name ?? "Unknown", "DeleteContact", "Contact", id, $"Deleted contact: {name}", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CreateGroup()
    {
        var viewModel = new ContactGroupEditViewModel
        {
            Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(ContactGroupEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var group = new ContactGroup
            {
                ApplicationId = model.ApplicationId,
                Name = model.Name,
                Description = model.Description
            };
            _context.ContactGroups.Add(group);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        model.Applications = await _context.Applications.Where(a => a.IsEnabled).ToListAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var group = await _context.ContactGroups.FindAsync(id);
        if (group != null)
        {
            _context.ContactGroups.Remove(group);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
