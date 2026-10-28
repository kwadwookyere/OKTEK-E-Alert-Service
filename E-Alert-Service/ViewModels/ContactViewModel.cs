using System.ComponentModel.DataAnnotations;
using EAlertService.Models.DomainModels;

namespace EAlertService.ViewModels;

public class ContactListViewModel : IPagedResult
{
    public List<Contact> Contacts { get; set; } = new();
    public List<ContactGroup> Groups { get; set; } = new();
    public List<Application> Applications { get; set; } = new();
    public int? SelectedGroupId { get; set; }
    public string? SearchTerm { get; set; }

    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalRecords { get; set; }

    public int TotalPages => TotalRecords > 0 ? (int)Math.Ceiling((double)TotalRecords / Math.Max(PageSize, 1)) : 1;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public int StartRecord => TotalRecords == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
    public int EndRecord => Math.Min(PageNumber * PageSize, TotalRecords);
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
