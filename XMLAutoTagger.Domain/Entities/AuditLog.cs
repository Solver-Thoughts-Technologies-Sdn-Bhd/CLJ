using System;

namespace XMLAutoTagger.Domain.Entities
{
    public class AuditLog
    {
        public Guid AuditLogId { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public Guid? ActorId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? ScreenCode { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Guid? SessionId { get; set; }
        public string Outcome { get; set; } = "success";
        public string? Detail { get; set; } = "{}";
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}

