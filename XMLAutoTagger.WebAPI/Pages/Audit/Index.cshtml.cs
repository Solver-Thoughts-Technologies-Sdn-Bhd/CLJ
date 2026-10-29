using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using XMLAutoTagger.Application.Features.AuditLogs.DTOs;
using XMLAutoTagger.Application.Features.AuditLogs.Queries;
using XMLAutoTagger.WebAPI.Services.ApiClients;

namespace XMLAutoTagger.WebAPI.Pages.Audit
{
    public class IndexModel : PageModel
    {
        private readonly IUserManagementApiClient _apiClient;

        public IndexModel(IUserManagementApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public AuditLogsResultDto Result { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Action { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EntityName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Outcome { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 25;

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Audit";
            ViewData["Title"] = "Audit Log";
            ViewData["PageHeading"] = "Audit log";

            var query = new GetAuditLogsQuery
            {
                Search = Search,
                Action = Action,
                EntityName = EntityName,
                Outcome = Outcome,
                FromDate = FromDate,
                ToDate = ToDate,
                PageNumber = PageNumber > 0 ? PageNumber : 1,
                PageSize = PageSize > 0 ? PageSize : 25
            };

            Result = await _apiClient.GetAuditLogsAsync(query);
            ViewData["PageSubtitle"] = $"{Result.TotalCount} audit events recorded across all entities & security operations";

            return Page();
        }

        public async Task<IActionResult> OnGetLogDetailJsonAsync(Guid id)
        {
            var log = await _apiClient.GetAuditLogByIdAsync(id);
            if (log == null)
            {
                return NotFound(new { message = "Audit log entry not found." });
            }

            return new JsonResult(log);
        }
    }
}

