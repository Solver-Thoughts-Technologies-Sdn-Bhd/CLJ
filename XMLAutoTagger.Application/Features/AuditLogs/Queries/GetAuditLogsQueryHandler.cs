using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using XMLAutoTagger.Application.Features.AuditLogs.DTOs;
using XMLAutoTagger.Domain.Interfaces;

namespace XMLAutoTagger.Application.Features.AuditLogs.Queries
{
    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, AuditLogsResultDto>
    {
        private readonly IApplicationDbContext _context;

        public GetAuditLogsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLogsResultDto> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Action))
            {
                var actionFilter = request.Action.Trim().ToLowerInvariant();
                query = query.Where(a => a.Action.ToLower().Contains(actionFilter));
            }

            if (!string.IsNullOrWhiteSpace(request.Outcome))
            {
                var outcomeFilter = request.Outcome.Trim().ToLowerInvariant();
                query = query.Where(a => a.Outcome.ToLower() == outcomeFilter);
            }

            if (!string.IsNullOrWhiteSpace(request.EntityName))
            {
                var entityFilter = request.EntityName.Trim().ToLowerInvariant();
                query = query.Where(a => a.Detail != null && a.Detail.ToLower().Contains($"\"entityname\":\"{entityFilter}\""));
            }

            if (request.FromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc);
                query = query.Where(a => a.OccurredAt >= fromUtc);
            }

            if (request.ToDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc);
                query = query.Where(a => a.OccurredAt <= toUtc);
            }

            if (request.ActorId.HasValue)
            {
                query = query.Where(a => a.ActorId == request.ActorId.Value);
            }

            if (request.UserId.HasValue)
            {
                query = query.Where(a => a.UserId == request.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim().ToLowerInvariant();
                query = query.Where(a =>
                    a.Action.ToLower().Contains(term) ||
                    (a.ScreenCode != null && a.ScreenCode.ToLower().Contains(term)) ||
                    (a.IpAddress != null && a.IpAddress.ToLower().Contains(term)) ||
                    (a.Detail != null && a.Detail.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var page = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 25;

            var logs = await query
                .OrderByDescending(a => a.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Fetch user display info for referenced user IDs
            var userIds = logs
                .Select(l => l.UserId)
                .Concat(logs.Select(l => l.ActorId))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var usersMap = new Dictionary<Guid, (string Email, string Name)>();
            if (userIds.Count > 0)
            {
                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.UserId))
                    .Select(u => new { u.UserId, u.Email, u.FullName })
                    .ToListAsync(cancellationToken);

                usersMap = users.ToDictionary(u => u.UserId, u => (u.Email, u.FullName));
            }

            var items = new List<AuditLogDto>();

            foreach (var log in logs)
            {
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
                    catch
                    {
                        // Ignore detail parsing errors
                    }
                }

                string? actorName = null;
                if (log.ActorId.HasValue && usersMap.TryGetValue(log.ActorId.Value, out var actorInfo))
                {
                    actorName = !string.IsNullOrWhiteSpace(actorInfo.Name) ? $"{actorInfo.Name} ({actorInfo.Email})" : actorInfo.Email;
                }
                else
                {
                    actorName = actorNameFromDetail ?? "System";
                }

                string? targetUserName = null;
                string? targetUserEmail = null;
                if (log.UserId.HasValue && usersMap.TryGetValue(log.UserId.Value, out var targetInfo))
                {
                    targetUserName = targetInfo.Name;
                    targetUserEmail = targetInfo.Email;
                }

                items.Add(new AuditLogDto
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
                });
            }

            return new AuditLogsResultDto
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }
    }
}

