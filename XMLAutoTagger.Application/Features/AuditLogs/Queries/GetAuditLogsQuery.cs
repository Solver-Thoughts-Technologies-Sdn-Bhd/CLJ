using System;
using MediatR;
using XMLAutoTagger.Application.Features.AuditLogs.DTOs;

namespace XMLAutoTagger.Application.Features.AuditLogs.Queries
{
    public class GetAuditLogsQuery : IRequest<AuditLogsResultDto>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? Search { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public string? Outcome { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? ActorId { get; set; }
        public Guid? UserId { get; set; }
    }
}

