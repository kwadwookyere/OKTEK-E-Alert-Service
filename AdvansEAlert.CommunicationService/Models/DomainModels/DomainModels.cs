using System.ComponentModel.DataAnnotations;

namespace AdvansEAlert.CommunicationService.Models.DomainModels;

public class Application
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ApiKeyHash { get; set; } = string.Empty;

    public string? ApiKeyEncrypted { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SmtpConfiguration> SmtpConfigurations { get; set; } = new List<SmtpConfiguration>();
    public ICollection<SmsProviderConfiguration> SmsProviders { get; set; } = new List<SmsProviderConfiguration>();
}


public class SmtpConfiguration
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordEncrypted { get; set; } = string.Empty;

    public bool EnableSsl { get; set; }

    [Required]
    [MaxLength(100)]
    public string FromEmail { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FromName { get; set; }

    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EmailTemplate
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyHtml { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class SmsProviderConfiguration
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string ProviderName { get; set; } = string.Empty; // e.g., "Twilio", "Hubtel"

    [Required]
    public string ApiKeyEncrypted { get; set; } = string.Empty;

    public string? ApiSecretEncrypted { get; set; }

    public string? SenderId { get; set; }

    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SmsTemplate
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EmailLog
{
    public long Id { get; set; }
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Recipient { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    public string? Body { get; set; }

    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed

    public string? ErrorMessage { get; set; }

    public int? UsedSmtpConfigurationId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SmsLog
{
    public long Id { get; set; }
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Recipient { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string? ErrorMessage { get; set; }

    public string? ProviderResponse { get; set; }

    public int? UsedSmsProviderId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class User
{
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class Role
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty; // e.g., "ManageApps", "ViewLogs"
}

public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

