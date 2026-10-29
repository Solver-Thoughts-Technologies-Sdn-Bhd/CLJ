using System;

namespace XMLAutoTagger.Application.Features.AuditLogs.DTOs
{
    public class AuditLogDto
    {
        public Guid AuditLogId { get; set; }
        public Guid? UserId { get; set; }
        public string? TargetUserEmail { get; set; }
        public string? TargetUserName { get; set; }
        public Guid? ActorId { get; set; }
        public string? ActorName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? ScreenCode { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Guid? SessionId { get; set; }
        public string Outcome { get; set; } = "success";
        public string? Detail { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}

