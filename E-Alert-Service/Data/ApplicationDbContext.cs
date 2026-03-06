using EAlertService.Models.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace EAlertService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Application> Applications { get; set; }
    public DbSet<SmtpConfiguration> SmtpConfigurations { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<SmsProviderConfiguration> SmsProviders { get; set; }
    public DbSet<SmsTemplate> SmsTemplates { get; set; }
    public DbSet<SmsLog> SmsLogs { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<ContactGroup> ContactGroups { get; set; }
    public DbSet<ContactGroupAssignment> ContactGroupAssignments { get; set; }
    public DbSet<ScheduledMessage> ScheduledMessages { get; set; }
    public DbSet<BulkMessageBatch> BulkMessageBatches { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<ContactGroupAssignment>()
            .HasKey(cga => new { cga.ContactId, cga.ContactGroupId });

        modelBuilder.Entity<ContactGroupAssignment>()
            .HasOne(cga => cga.Contact)
            .WithMany(c => c.GroupAssignments)
            .HasForeignKey(cga => cga.ContactId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ContactGroupAssignment>()
            .HasOne(cga => cga.ContactGroup)
            .WithMany(g => g.ContactAssignments)
            .HasForeignKey(cga => cga.ContactGroupId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasIndex(a => a.Name).IsUnique();
        });

        // Seed Permissions
        var permissions = new[]
        {
            new Permission { Id = 1, Name = "AdminAccess" },
            new Permission { Id = 2, Name = "ManageApps" },
            new Permission { Id = 3, Name = "ViewLogs" }
        };
        modelBuilder.Entity<Permission>().HasData(permissions);

        // Seed Admin Role
        modelBuilder.Entity<Role>().HasData(new Role { Id = 1, Name = "SuperAdmin" });

        // Link Role to Permissions
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = 1, PermissionId = 1 },
            new RolePermission { RoleId = 1, PermissionId = 2 },
            new RolePermission { RoleId = 1, PermissionId = 3 }
        );

        // Seed Default Admin User
        var adminUser = new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@advansio.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1.com"),
            IsActive = true
        };
        modelBuilder.Entity<User>().HasData(adminUser);

        // Link User to Role
        modelBuilder.Entity<UserRole>().HasData(new UserRole { UserId = 1, RoleId = 1 });

        modelBuilder.Entity<SmtpConfiguration>(entity =>
        {
            entity.HasOne(d => d.Application)
                .WithMany(p => p.SmtpConfigurations)
                .HasForeignKey(d => d.ApplicationId);
        });

        modelBuilder.Entity<SmsProviderConfiguration>(entity =>
        {
            entity.HasOne(d => d.Application)
                .WithMany(p => p.SmsProviders)
                .HasForeignKey(d => d.ApplicationId);
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasOne(d => d.Application)
                .WithMany()
                .HasForeignKey(d => d.ApplicationId);
        });

        modelBuilder.Entity<SmsTemplate>(entity =>
        {
            entity.HasOne(d => d.Application)
                .WithMany()
                .HasForeignKey(d => d.ApplicationId);
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<SmsLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Status);
        });
    }
}

