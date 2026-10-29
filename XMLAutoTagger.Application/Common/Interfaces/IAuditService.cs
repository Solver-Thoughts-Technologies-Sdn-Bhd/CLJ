using System;
using System.Threading;
using System.Threading.Tasks;

namespace XMLAutoTagger.Application.Common.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(
            string action,
            string? entityName = null,
            string? entityId = null,
            Guid? targetUserId = null,
            string outcome = "success",
            object? details = null,
            string? screenCode = null,
            CancellationToken ct = default);
    }
}

