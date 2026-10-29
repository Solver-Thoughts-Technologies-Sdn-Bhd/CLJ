using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using XMLAutoTagger.Application.Common.Interfaces;
using XMLAutoTagger.Domain.Entities;
using XMLAutoTagger.Domain.Interfaces;

namespace XMLAutoTagger.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<AuditService> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task LogAsync(
            string action,
            string? entityName = null,
            string? entityId = null,
            Guid? targetUserId = null,
            string outcome = "success",
            object? details = null,
            string? screenCode = null,
            CancellationToken ct = default)
        {
            try
            {
                string detailJson = "{}";
                if (details != null)
                {
                    if (details is string str && (str.StartsWith("{") || str.StartsWith("[")))
                    {
                        detailJson = str;
                    }
                    else
                    {
                        detailJson = JsonSerializer.Serialize(details);
                    }
                }

                var log = new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    UserId = targetUserId,
                    ActorId = _currentUserService.UserId,
                    Action = action,
                    ScreenCode = screenCode,
                    IpAddress = _currentUserService.IpAddress,
                    UserAgent = _currentUserService.UserAgent,
                    Outcome = outcome,
                    Detail = detailJson,
                    OccurredAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
            }
        }
    }
}

