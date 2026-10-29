using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XMLAutoTagger.Domain.Entities;

namespace XMLAutoTagger.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("user_audit_log");

            builder.HasKey(a => a.AuditLogId);
            builder.Property(a => a.AuditLogId).HasColumnName("audit_log_id").ValueGeneratedNever();

            builder.Property(a => a.UserId).HasColumnName("users_id");
            builder.Property(a => a.ActorId).HasColumnName("actor_id");
            builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(80).IsRequired();
            builder.Property(a => a.ScreenCode).HasColumnName("screen_code").HasMaxLength(80);
            builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
            builder.Property(a => a.UserAgent).HasColumnName("user_agent");
            builder.Property(a => a.SessionId).HasColumnName("session_id");
            builder.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(20).IsRequired().HasDefaultValue("success");
            builder.Property(a => a.Detail).HasColumnName("detail").HasColumnType("jsonb");
            builder.Property(a => a.OccurredAt).HasColumnName("occurred_at").IsRequired();

            builder.HasIndex(a => a.OccurredAt).HasDatabaseName("idx_uaudit_date");
            builder.HasIndex(a => a.ActorId).HasDatabaseName("idx_uaudit_actor");
            builder.HasIndex(a => a.UserId).HasDatabaseName("idx_uaudit_user");
            builder.HasIndex(a => a.Action).HasDatabaseName("idx_uaudit_action");
            builder.HasIndex(a => a.SessionId).HasDatabaseName("idx_uaudit_session");
        }
    }
}

