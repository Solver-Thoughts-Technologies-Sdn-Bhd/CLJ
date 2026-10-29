using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using XMLAutoTagger.Application.Common.Interfaces;
using XMLAutoTagger.Domain.Common;
using XMLAutoTagger.Domain.Entities;
using XMLAutoTagger.Domain.Interfaces;
using XMLAutoTagger.Infrastructure.Persistence.Audit;
using XMLAutoTagger.Infrastructure.Persistence.Extensions;

namespace XMLAutoTagger.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly ICurrentUserService _currentUserService;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Screen> Screens => Set<Screen>();
        public DbSet<RoleScreenPermission> RoleScreenPermissions => Set<RoleScreenPermission>();
        public DbSet<UserScreenOverride> UserScreenOverrides => Set<UserScreenOverride>();
        public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
        public DbSet<EmailSetting> EmailSettings => Set<EmailSetting>();
        public DbSet<SmsSetting> SmsSettings => Set<SmsSetting>();
        public DbSet<WhatsAppSetting> WhatsAppSettings => Set<WhatsAppSetting>();
        public DbSet<CommunicationEventItem> CommunicationEvents => Set<CommunicationEventItem>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.UserId;
            var currentUserName = _currentUserService.Username;
            var ipAddress = _currentUserService.IpAddress;
            var userAgent = _currentUserService.UserAgent;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.CreatedBy = currentUserId;
                        entry.Entity.UpdatedBy = currentUserId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = currentUserId;
                        entry.Property(e => e.CreatedAt).IsModified = false;
                        entry.Property(e => e.CreatedBy).IsModified = false;
                        break;
                }
            }

            // 1. Prepare audit entries before saving changes
            var auditEntries = OnBeforeSaveChanges(currentUserId, currentUserName, ipAddress, userAgent);

            // 2. Commit primary entity changes
            var result = await base.SaveChangesAsync(cancellationToken);

            // 3. Write and commit audit logs
            if (auditEntries != null && auditEntries.Count > 0)
            {
                await OnAfterSaveChangesAsync(auditEntries, cancellationToken);
            }

            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges(Guid? actorId, string? actorName, string? ipAddress, string? userAgent)
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var entityType = entry.Entity.GetType();
                var entityName = entityType.Name;
                if (entityName.Contains("Proxy"))
                {
                    entityName = entityType.BaseType?.Name ?? entityName;
                }

                var auditEntry = new AuditEntry(entry)
                {
                    EntityName = entityName,
                    ActorId = actorId,
                    ActorName = actorName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Outcome = "success"
                };

                // Determine screen code based on entity
                auditEntry.ScreenCode = entityName switch
                {
                    nameof(User) => "user_management",
                    nameof(UserCredential) => "user_management",
                    nameof(UserScreenOverride) => "user_management",
                    nameof(Role) => "user_management",
                    nameof(RoleScreenPermission) => "user_management",
                    nameof(Department) => "user_management",
                    nameof(DepartmentMember) => "user_management",
                    nameof(EmailSetting) => "settings",
                    nameof(SmsSetting) => "settings",
                    nameof(WhatsAppSetting) => "settings",
                    nameof(CommunicationEventItem) => "settings",
                    nameof(Screen) => "settings",
                    _ => "system"
                };

                // Determine target user ID if applicable
                if (entry.Entity is User u)
                {
                    auditEntry.TargetUserId = u.UserId;
                }
                else if (entry.Entity is UserCredential uc)
                {
                    auditEntry.TargetUserId = uc.UserId;
                }
                else if (entry.Entity is UserScreenOverride uso)
                {
                    auditEntry.TargetUserId = uso.UserId;
                }
                else
                {
                    var userProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UserId");
                    if (userProp?.CurrentValue is Guid guidVal)
                    {
                        auditEntry.TargetUserId = guidVal;
                    }
                }

                // Determine Action name
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Action = entityName switch
                        {
                            nameof(User) => "user_created",
                            nameof(Role) => "role_created",
                            nameof(Department) => "department_created",
                            nameof(RoleScreenPermission) => "permission_granted",
                            nameof(UserScreenOverride) => "permission_granted",
                            _ => $"{entityName.ToLower()}_created"
                        };
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                            }
                            var val = AuditEntry.IsSensitive(prop.Metadata.Name) ? "[PROTECTED]" : prop.CurrentValue;
                            auditEntry.NewValues[prop.Metadata.Name] = val;
                        }
                        break;

                    case EntityState.Deleted:
                        auditEntry.Action = entityName switch
                        {
                            nameof(User) => "user_deactivated",
                            nameof(Role) => "role_deleted",
                            nameof(Department) => "department_deleted",
                            nameof(RoleScreenPermission) => "permission_revoked",
                            nameof(UserScreenOverride) => "permission_revoked",
                            _ => $"{entityName.ToLower()}_deleted"
                        };
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                auditEntry.KeyValues[prop.Metadata.Name] = prop.OriginalValue;
                            }
                            var val = AuditEntry.IsSensitive(prop.Metadata.Name) ? "[PROTECTED]" : prop.OriginalValue;
                            auditEntry.OldValues[prop.Metadata.Name] = val;
                        }
                        break;

                    case EntityState.Modified:
                        if (entry.Entity is User userEntity)
                        {
                            var statusProp = entry.Property(nameof(User.AccountStatus));
                            if (statusProp.IsModified)
                            {
                                var newStatus = statusProp.CurrentValue?.ToString();
                                if (string.Equals(newStatus, "locked", StringComparison.OrdinalIgnoreCase))
                                    auditEntry.Action = "account_locked";
                                else if (string.Equals(newStatus, "active", StringComparison.OrdinalIgnoreCase))
                                    auditEntry.Action = "account_unlocked";
                                else
                                    auditEntry.Action = "user_updated";
                            }
                            else
                            {
                                auditEntry.Action = "user_updated";
                            }
                        }
                        else
                        {
                            auditEntry.Action = entityName switch
                            {
                                nameof(Role) => "role_updated",
                                nameof(Department) => "department_updated",
                                nameof(RoleScreenPermission) => "permission_updated",
                                nameof(UserScreenOverride) => "permission_updated",
                                _ => $"{entityName.ToLower()}_updated"
                            };
                        }

                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                            }
                            if (prop.IsModified)
                            {
                                auditEntry.ChangedColumns.Add(prop.Metadata.Name);
                                auditEntry.OldValues[prop.Metadata.Name] = AuditEntry.IsSensitive(prop.Metadata.Name) ? "[PROTECTED]" : prop.OriginalValue;
                                auditEntry.NewValues[prop.Metadata.Name] = AuditEntry.IsSensitive(prop.Metadata.Name) ? "[PROTECTED]" : prop.CurrentValue;
                            }
                        }
                        break;
                }

                auditEntries.Add(auditEntry);
            }

            return auditEntries;
        }

        private async Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
        {
            if (auditEntries == null || auditEntries.Count == 0) return;

            try
            {
                var logs = new List<AuditLog>();
                foreach (var auditEntry in auditEntries)
                {
                    if (auditEntry.Entry.State == EntityState.Added || auditEntry.KeyValues.Count == 0)
                    {
                        foreach (var prop in auditEntry.Entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                            }
                        }
                    }

                    logs.Add(auditEntry.ToAuditLog());
                }

                AuditLogs.AddRange(logs);
                await base.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Suppress audit log write failures to ensure primary business persistence is not rolled back
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("user_mgmt");

            // Automatically discover and apply all entity configurations from Configurations/
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Seed reference & initial data
            modelBuilder.Seed();
        }
    }
}
