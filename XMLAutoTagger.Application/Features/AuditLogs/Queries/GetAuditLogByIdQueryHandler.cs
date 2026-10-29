using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using XMLAutoTagger.Application.Features.AuditLogs.DTOs;
using XMLAutoTagger.Domain.Interfaces;

namespace XMLAutoTagger.Application.Features.AuditLogs.Queries
{
    public class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, AuditLogDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetAuditLogByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLogDto?> Handle(GetAuditLogByIdQuery request, CancellationToken cancellationToken)
        {
            var log = await _context.AuditLogs.AsNoTracking()
                .FirstOrDefaultAsync(a => a.AuditLogId == request.Id, cancellationToken);

            if (log == null) return null;

            string? entityName = null;
            string? entityId = null;
            string? actorNameFromDetail = null;

            if (!string.IsNullOrWhiteSpace(log.Detail))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.Detail);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("entityName", out var en)) entityName = en.GetString();
                    if (root.TryGetProperty("entityId", out var ei)) entityId = ei.GetString();
                    if (root.TryGetProperty("actorName", out var an)) actorNameFromDetail = an.GetString();
                }
                catch { }
            }

            string? actorName = actorNameFromDetail ?? "System";
            if (log.ActorId.HasValue)
            {
                var actor = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == log.ActorId.Value, cancellationToken);
                if (actor != null)
                {
                    actorName = !string.IsNullOrWhiteSpace(actor.FullName) ? $"{actor.FullName} ({actor.Email})" : actor.Email;
                }
            }

            string? targetUserName = null;
            string? targetUserEmail = null;
            if (log.UserId.HasValue)
            {
                var target = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == log.UserId.Value, cancellationToken);
                if (target != null)
                {
                    targetUserName = target.FullName;
                    targetUserEmail = target.Email;
                }
            }

            return new AuditLogDto
            {
                AuditLogId = log.AuditLogId,
                UserId = log.UserId,
                TargetUserName = targetUserName,
                TargetUserEmail = targetUserEmail,
                ActorId = log.ActorId,
                ActorName = actorName,
                Action = log.Action,
                EntityName = entityName,
                EntityId = entityId,
                ScreenCode = log.ScreenCode,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                SessionId = log.SessionId,
                Outcome = log.Outcome,
                Detail = log.Detail,
                OccurredAt = log.OccurredAt
            };
        }
    }
}

