using System.ComponentModel.DataAnnotations;
using EAlertService.Models.DomainModels;

namespace EAlertService.ViewModels;

public class ContactListViewModel
{
    public List<Contact> Contacts { get; set; } = new();
    public List<ContactGroup> Groups { get; set; } = new();
    public List<Application> Applications { get; set; } = new();
}

public class ContactEditViewModel
{
    public int Id { get; set; }
    
    [Required]
    public int ApplicationId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public List<int> SelectedGroupIds { get; set; } = new();
    
    public List<Application> Applications { get; set; } = new();
    public List<ContactGroup> AllGroups { get; set; } = new();
}

public class ContactGroupEditViewModel
{
    public int Id { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    public List<Application> Applications { get; set; } = new();
}
