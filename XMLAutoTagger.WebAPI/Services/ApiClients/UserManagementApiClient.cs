using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    public class UserManagementApiClient : IUserManagementApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<UserManagementApiClient> _logger;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public UserManagementApiClient(HttpClient http, ILogger<UserManagementApiClient> logger, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _http = http;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;

            // Auto-attach Bearer token from cookie if available in current browser session
            var cookieToken = _httpContextAccessor.HttpContext?.Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(cookieToken))
            {
                SetBearerToken(cookieToken);
            }
        }

        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("error", out var errElement) && !string.IsNullOrWhiteSpace(errElement.GetString()))
                    {
                        throw new InvalidOperationException(errElement.GetString());
                    }
                    if (doc.RootElement.TryGetProperty("message", out var msgElement) && !string.IsNullOrWhiteSpace(msgElement.GetString()))
                    {
                        throw new InvalidOperationException(msgElement.GetString());
                    }
                }
                catch (JsonException)
                {
                    // Fall back to raw content if not a JSON object
                }
                throw new InvalidOperationException(content);
            }

            throw new InvalidOperationException($"API request failed with HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }

        // --- USERS ---
        public async Task<List<UserDto>> GetUsersAsync(string? search = null, string? status = null, Guid? departmentId = null, Guid? roleId = null)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
            if (departmentId.HasValue) query.Add($"departmentId={departmentId.Value}");
            if (roleId.HasValue) query.Add($"roleId={roleId.Value}");

            var url = "api/v1/users" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            return await _http.GetFromJsonAsync<List<UserDto>>(url, _jsonOptions) ?? new List<UserDto>();
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            return await _http.GetFromJsonAsync<UserDto>($"api/v1/users/{id}", _jsonOptions);
        }

        public async Task<Guid> CreateUserAsync(CreateUserCommand command)
        {
            var resp = await _http.PostAsJsonAsync("api/v1/users", command);
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("userId").GetGuid();
        }

        public async Task<bool> UpdateUserAsync(Guid id, UpdateUserCommand command)
        {
            var resp = await _http.PutAsJsonAsync($"api/v1/users/{id}", command);
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("success").GetBoolean();
        }

        public async Task<string> ToggleUserStatusAsync(Guid id)
        {
            var resp = await _http.PatchAsync($"api/v1/users/{id}/status", null);
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("newStatus").GetString() ?? "active";
        }

        // --- DEPARTMENTS ---
        public async Task<List<DepartmentDto>> GetDepartmentsAsync()
        {
            return await _http.GetFromJsonAsync<List<DepartmentDto>>("api/v1/departments", _jsonOptions) ?? new List<DepartmentDto>();
        }

        public async Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id)
        {
            return await _http.GetFromJsonAsync<DepartmentDto>($"api/v1/departments/{id}", _jsonOptions);
        }

        public async Task<Guid> CreateDepartmentAsync(CreateDepartmentCommand command)
        {
            var resp = await _http.PostAsJsonAsync("api/v1/departments", command);
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("departmentId").GetGuid();
        }

        public async Task<bool> UpdateDepartmentAsync(Guid id, UpdateDepartmentCommand command)
        {
            var resp = await _http.PutAsJsonAsync($"api/v1/departments/{id}", command);
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("success").GetBoolean();
        }

        public async Task<bool> DeleteDepartmentAsync(Guid id)
        {
            var resp = await _http.DeleteAsync($"api/v1/departments/{id}");
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("success").GetBoolean();
        }

        public async Task<bool> AssignDepartmentMemberAsync(Guid departmentId, Guid userId)
        {
            var resp = await _http.PostAsJsonAsync($"api/v1/departments/{departmentId}/members", new { userId });
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("success").GetBoolean();
        }

        public async Task<bool> RemoveDepartmentMemberAsync(Guid departmentId, Guid userId)
        {
            var resp = await _http.DeleteAsync($"api/v1/departments/{departmentId}/members/{userId}");
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("success").GetBoolean();
        }

        // --- ROLES ---
        public async Task<List<RoleDto>> GetRolesAsync()
        {
            return await _http.GetFromJsonAsync<List<RoleDto>>("api/v1/roles", _jsonOptions) ?? new List<RoleDto>();
        }

        public async Task<RoleDto?> GetRoleByIdAsync(Guid id)
        {
            return await _http.GetFromJsonAsync<RoleDto>($"api/v1/roles/{id}", _jsonOptions);
        }

        public async Task<Guid> CreateRoleAsync(CreateRoleCommand command)
        {
            var resp = await _http.PostAsJsonAsync("api/v1/roles", command);
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("roleId").GetGuid();
        }

        public async Task<bool> UpdateRoleAsync(Guid id, UpdateRoleCommand command)
        {
            var resp = await _http.PutAsJsonAsync($"api/v1/roles/{id}", command);
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("success").GetBoolean();
        }

        public async Task<bool> DeleteRoleAsync(Guid id)
        {
            var resp = await _http.DeleteAsync($"api/v1/roles/{id}");
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("success").GetBoolean();
        }

        // --- SCREEN ACCESS ---
        public async Task<List<Screen>> GetScreensAsync()
        {
            return await _http.GetFromJsonAsync<List<Screen>>("api/v1/screen-access/screens", _jsonOptions) ?? new List<Screen>();
        }

        public async Task<UserAccessDetailDto?> GetUserAccessAsync(Guid userId)
        {
            return await _http.GetFromJsonAsync<UserAccessDetailDto>($"api/v1/screen-access/users/{userId}", _jsonOptions);
        }

        public async Task<bool> SaveUserAccessAsync(Guid userId, SaveUserAccessCommand command)
        {
            var resp = await _http.PostAsJsonAsync($"api/v1/screen-access/users/{userId}", command);
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("success").GetBoolean();
        }

        public async Task<bool> ResetUserAccessAsync(Guid userId)
        {
            var resp = await _http.PostAsync($"api/v1/screen-access/users/{userId}/reset", null);
            await EnsureSuccessOrThrowAsync(resp);
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return doc.GetProperty("success").GetBoolean();
        }

        public async Task<XMLAutoTagger.Application.Features.Auth.DTOs.LoginResponseDto> LoginAsync(XMLAutoTagger.Application.Features.Auth.DTOs.LoginRequestDto request)
        {
            var resp = await _http.PostAsJsonAsync("api/v1/auth/login", request, _jsonOptions);
            await EnsureSuccessOrThrowAsync(resp);
            var result = await resp.Content.ReadFromJsonAsync<XMLAutoTagger.Application.Features.Auth.DTOs.LoginResponseDto>(_jsonOptions);
            if (result != null && !string.IsNullOrEmpty(result.Token))
            {
                SetBearerToken(result.Token);
            }
            return result!;
        }

        public async Task<object?> GetCurrentUserAsync()
        {
            var resp = await _http.GetAsync("api/v1/auth/me");
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<object>(_jsonOptions);
        }

        public void SetBearerToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _http.DefaultRequestHeaders.Authorization = null;
            }
            else
            {
                var cleanToken = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? token.Substring(7).Trim()
                    : token.Trim();
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cleanToken);
            }
        }

        private void EnsureAuthHeader()
        {
            var cookieToken = _httpContextAccessor.HttpContext?.Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(cookieToken))
            {
                SetBearerToken(cookieToken);
            }
        }

        public async Task<UserDto?> GetMyProfileAsync()
        {
            EnsureAuthHeader();
            var resp = await _http.GetAsync("api/v1/profile");
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);
        }

        public async Task<bool> UpdateMyProfileAsync(string? phone, string? preferredLanguage = null)
        {
            EnsureAuthHeader();
            var resp = await _http.PutAsJsonAsync("api/v1/profile", new { phone, preferredLanguage });
            await EnsureSuccessOrThrowAsync(resp);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> ChangeMyPasswordAsync(string currentPassword, string newPassword, string confirmPassword)
        {
            EnsureAuthHeader();
            var resp = await _http.PostAsJsonAsync("api/v1/profile/change-password", new
            {
                currentPassword,
                newPassword,
                confirmPassword
            });
            await EnsureSuccessOrThrowAsync(resp);
            return resp.IsSuccessStatusCode;
        }

        // --- AUDIT LOGS ---
        public async Task<AuditLogsResultDto> GetAuditLogsAsync(GetAuditLogsQuery query)
        {
            EnsureAuthHeader();
            var queryString = new List<string>();
            queryString.Add($"pageNumber={query.PageNumber}");
            queryString.Add($"pageSize={query.PageSize}");

            if (!string.IsNullOrWhiteSpace(query.Search))
                queryString.Add($"search={Uri.EscapeDataString(query.Search)}");
            if (!string.IsNullOrWhiteSpace(query.Action))
                queryString.Add($"action={Uri.EscapeDataString(query.Action)}");
            if (!string.IsNullOrWhiteSpace(query.EntityName))
                queryString.Add($"entityName={Uri.EscapeDataString(query.EntityName)}");
            if (!string.IsNullOrWhiteSpace(query.Outcome))
                queryString.Add($"outcome={Uri.EscapeDataString(query.Outcome)}");
            if (query.FromDate.HasValue)
                queryString.Add($"fromDate={query.FromDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (query.ToDate.HasValue)
                queryString.Add($"toDate={query.ToDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (query.ActorId.HasValue)
                queryString.Add($"actorId={query.ActorId.Value}");
            if (query.UserId.HasValue)
                queryString.Add($"userId={query.UserId.Value}");

            var url = "api/v1/audit-logs?" + string.Join("&", queryString);
            return await _http.GetFromJsonAsync<AuditLogsResultDto>(url, _jsonOptions) ?? new AuditLogsResultDto();
        }

        public async Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id)
        {
            EnsureAuthHeader();
            var resp = await _http.GetAsync($"api/v1/audit-logs/{id}");
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<AuditLogDto>(_jsonOptions);
        }
    }
}
