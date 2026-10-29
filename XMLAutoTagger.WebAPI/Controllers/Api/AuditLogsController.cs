using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using XMLAutoTagger.Application.Features.AuditLogs.DTOs;
using XMLAutoTagger.Application.Features.AuditLogs.Queries;

namespace XMLAutoTagger.WebAPI.Controllers.Api
{
    [ApiController]
    [Route("api/v1/audit-logs")]
    [Produces("application/json")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Retrieves paginated audit logs with search, action, entity, and date filtering.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(AuditLogsResultDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuditLogsResultDto>> GetAuditLogs([FromQuery] GetAuditLogsQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves an audit log record by its ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AuditLogDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AuditLogDto>> GetAuditLogById(Guid id)
        {
            var result = await _mediator.Send(new GetAuditLogByIdQuery(id));
            if (result == null)
            {
                return NotFound(new { message = $"Audit log with ID {id} not found." });
            }

            return Ok(result);
        }
    }
}

