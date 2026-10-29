using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using XMLAutoTagger.Domain.Entities;

namespace XMLAutoTagger.Domain.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<Department> Departments { get; }
        DbSet<Screen> Screens { get; }
        DbSet<RoleScreenPermission> RoleScreenPermissions { get; }
        DbSet<UserScreenOverride> UserScreenOverrides { get; }
        DbSet<UserCredential> UserCredentials { get; }
        DbSet<EmailSetting> EmailSettings { get; }
        DbSet<SmsSetting> SmsSettings { get; }
        DbSet<WhatsAppSetting> WhatsAppSettings { get; }
        DbSet<CommunicationEventItem> CommunicationEvents { get; }
        DbSet<AuditLog> AuditLogs { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
