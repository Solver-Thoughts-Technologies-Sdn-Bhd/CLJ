using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using XMLAutoTagger.Domain.Entities;

namespace XMLAutoTagger.Infrastructure.Persistence.Audit
{
    public class AuditEntry
    {
        public EntityEntry Entry { get; }
        public string EntityName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Guid? TargetUserId { get; set; }
        public Guid? ActorId { get; set; }
        public string? ActorName { get; set; }
        public string? ScreenCode { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string Outcome { get; set; } = "success";

        public Dictionary<string, object?> KeyValues { get; } = new();
        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();
        public List<string> ChangedColumns { get; } = new();

        private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "passwordhash",
            "passwordencrypted",
            "apikeyencrypted",
            "authtokenencrypted",
            "accesstokenencrypted",
            "webhooksecretencrypted",
            "mfasecretenc",
            "mfasecret",
            "token"
        };

        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public static bool IsSensitive(string propertyName)
        {
            return SensitiveProperties.Any(s => propertyName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public AuditLog ToAuditLog()
        {
            var entityId = KeyValues.Count > 0 ? string.Join(",", KeyValues.Values.Select(v => v?.ToString())) : null;

            var detailObj = new Dictionary<string, object?>
            {
                ["entityName"] = EntityName,
                ["entityId"] = entityId,
                ["actorName"] = ActorName ?? "System",
                ["changedFields"] = ChangedColumns,
                ["oldValues"] = OldValues,
                ["newValues"] = NewValues
            };

            var detailJson = JsonSerializer.Serialize(detailObj);

            return new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                UserId = TargetUserId,
                ActorId = ActorId,
                Action = Action,
                ScreenCode = ScreenCode,
                IpAddress = IpAddress,
                UserAgent = UserAgent,
                Outcome = Outcome,
                Detail = detailJson,
                OccurredAt = DateTime.UtcNow
            };
        }
    }
}

