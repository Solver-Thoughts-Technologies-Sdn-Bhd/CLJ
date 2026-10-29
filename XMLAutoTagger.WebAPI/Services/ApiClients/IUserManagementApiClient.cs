using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XMLAutoTagger.Application.Features.AuditLogs.DTOs;
using XMLAutoTagger.Application.Features.AuditLogs.Queries;
using XMLAutoTagger.Application.Features.Departments.Commands;
using XMLAutoTagger.Application.Features.Departments.DTOs;
using XMLAutoTagger.Application.Features.Roles.Commands;
using XMLAutoTagger.Application.Features.Roles.DTOs;
using XMLAutoTagger.Application.Features.ScreenAccess.Commands;
using XMLAutoTagger.Application.Features.ScreenAccess.DTOs;
using XMLAutoTagger.Application.Features.Users.Commands;
using XMLAutoTagger.Application.Features.Users.DTOs;
using XMLAutoTagger.Domain.Entities;

namespace XMLAutoTagger.WebAPI.Services.ApiClients
{
    public interface IUserManagementApiClient
    {
        // Users
        Task<List<UserDto>> GetUsersAsync(string? search = null, string? status = null, Guid? departmentId = null, Guid? roleId = null);
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<Guid> CreateUserAsync(CreateUserCommand command);
        Task<bool> UpdateUserAsync(Guid id, UpdateUserCommand command);
        Task<string> ToggleUserStatusAsync(Guid id);

        // Departments
        Task<List<DepartmentDto>> GetDepartmentsAsync();
        Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id);
        Task<Guid> CreateDepartmentAsync(CreateDepartmentCommand command);
        Task<bool> UpdateDepartmentAsync(Guid id, UpdateDepartmentCommand command);
        Task<bool> DeleteDepartmentAsync(Guid id);
        Task<bool> AssignDepartmentMemberAsync(Guid departmentId, Guid userId);
        Task<bool> RemoveDepartmentMemberAsync(Guid departmentId, Guid userId);

        // Roles
        Task<List<RoleDto>> GetRolesAsync();
        Task<RoleDto?> GetRoleByIdAsync(Guid id);
        Task<Guid> CreateRoleAsync(CreateRoleCommand command);
        Task<bool> UpdateRoleAsync(Guid id, UpdateRoleCommand command);
        Task<bool> DeleteRoleAsync(Guid id);

        // Screen Access
        Task<List<Screen>> GetScreensAsync();
        Task<UserAccessDetailDto?> GetUserAccessAsync(Guid userId);
        Task<bool> SaveUserAccessAsync(Guid userId, SaveUserAccessCommand command);
        Task<bool> ResetUserAccessAsync(Guid userId);

        // Authentication (Option A: Bearer is handled automatically behind the scenes)
        Task<XMLAutoTagger.Application.Features.Auth.DTOs.LoginResponseDto> LoginAsync(XMLAutoTagger.Application.Features.Auth.DTOs.LoginRequestDto request);
        Task<object?> GetCurrentUserAsync();
        void SetBearerToken(string? token);

        // Profile & Self-Service
        Task<UserDto?> GetMyProfileAsync();
        Task<bool> UpdateMyProfileAsync(string? phone, string? preferredLanguage = null);
        Task<bool> ChangeMyPasswordAsync(string currentPassword, string newPassword, string confirmPassword);

        // Audit Logs
        Task<AuditLogsResultDto> GetAuditLogsAsync(GetAuditLogsQuery query);
        Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id);
    }
}
