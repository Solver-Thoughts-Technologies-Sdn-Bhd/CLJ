using System.Collections.Generic;

namespace XMLAutoTagger.Application.Features.AuditLogs.DTOs
{
    public class AuditLogsResultDto
    {
        public List<AuditLogDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (TotalCount + PageSize - 1) / (PageSize > 0 ? PageSize : 1);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}

