using System;

namespace XMLAutoTagger.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? Username { get; }
        string? RoleCode { get; }
        string? RoleName { get; }
        bool IsAuthenticated { get; }
        string? IpAddress { get; }
        string? UserAgent { get; }
    }
}
