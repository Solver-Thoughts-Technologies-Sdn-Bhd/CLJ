using System;
using MediatR;
using XMLAutoTagger.Application.Features.AuditLogs.DTOs;

namespace XMLAutoTagger.Application.Features.AuditLogs.Queries
{
    public record GetAuditLogByIdQuery(Guid Id) : IRequest<AuditLogDto?>;
}

