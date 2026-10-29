using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using XMLAutoTagger.Application.Common.Interfaces;
using XMLAutoTagger.Application.Features.Auth.DTOs;
using XMLAutoTagger.Domain.Enums;
using XMLAutoTagger.Domain.Interfaces;

namespace XMLAutoTagger.Application.Features.Auth.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ICommunicationService _communicationService;
        private readonly IAuditService _auditService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IApplicationDbContext context,
            IJwtService jwtService,
            ICommunicationService communicationService,
            IAuditService auditService,
            ILogger<LoginCommandHandler> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _communicationService = communicationService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Email and password are required.");
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .Include(u => u.Credential)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

            if (user == null)
            {
                await _auditService.LogAsync(
                    action: "login_failed",
                    entityName: "User",
                    outcome: "failure",
                    details: new { email = normalizedEmail, reason = "User not found" },
                    screenCode: "login",
                    ct: cancellationToken);

                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (user.AccountStatus.Equals("locked", StringComparison.OrdinalIgnoreCase))
            {
                await _auditService.LogAsync(
                    action: "login_failed",
                    entityName: "User",
                    entityId: user.UserId.ToString(),
                    targetUserId: user.UserId,
                    outcome: "failure",
                    details: new { email = normalizedEmail, reason = "Account is locked" },
                    screenCode: "login",
                    ct: cancellationToken);

                throw new UnauthorizedAccessException("Account is locked due to too many failed attempts. Please contact administrator.");
            }

            if (!user.AccountStatus.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                await _auditService.LogAsync(
                    action: "login_failed",
                    entityName: "User",
                    entityId: user.UserId.ToString(),
                    targetUserId: user.UserId,
                    outcome: "failure",
                    details: new { email = normalizedEmail, reason = $"Account status is {user.AccountStatus}" },
                    screenCode: "login",
                    ct: cancellationToken);

                throw new UnauthorizedAccessException($"Account is {user.AccountStatus}. Please contact administrator.");
            }

            if (user.Credential == null || string.IsNullOrWhiteSpace(user.Credential.PasswordHash))
            {
                await _auditService.LogAsync(
                    action: "login_failed",
                    entityName: "User",
                    entityId: user.UserId.ToString(),
                    targetUserId: user.UserId,
                    outcome: "failure",
                    details: new { email = normalizedEmail, reason = "No password credential configured" },
                    screenCode: "login",
                    ct: cancellationToken);

                throw new UnauthorizedAccessException("Account has no local password configured.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Credential.PasswordHash);

            if (!isPasswordValid)
            {
                user.FailedLoginCount++;
                bool justLocked = false;
                if (user.FailedLoginCount >= 5 && user.AccountStatus != "locked")
                {
                    user.AccountStatus = "locked";
                    user.LockedAt = DateTime.UtcNow;
                    user.LockedReason = "Exceeded maximum failed login attempts";
                    justLocked = true;
                }

                await _context.SaveChangesAsync(cancellationToken);

                await _auditService.LogAsync(
                    action: "login_failed",
                    entityName: "User",
                    entityId: user.UserId.ToString(),
                    targetUserId: user.UserId,
                    outcome: "failure",
                    details: new { email = normalizedEmail, reason = "Invalid password", failedAttempts = user.FailedLoginCount, accountLocked = justLocked },
                    screenCode: "login",
                    ct: cancellationToken);

                if (justLocked)
                {
                    try
                    {
                        var templateData = new Dictionary<string, string>
                        {
                            { "FullName", user.FullName },
                            { "Email", user.Email },
                            { "LockedReason", user.LockedReason ?? "Exceeded maximum failed login attempts" },
                            { "LockedAt", user.LockedAt?.ToString("dd MMM yyyy, HH:mm") ?? DateTime.UtcNow.ToString("dd MMM yyyy, HH:mm") },
                            { "AppName", "LegalXML" }
                        };

                        _logger.LogInformation("Sending account lockout email to {Email} via Event 4 (UserAccountLocked)", user.Email);
                        await _communicationService.SendEventNotificationAsync(
                            CommunicationEvent.UserAccountLocked,
                            user.Email,
                            templateData,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send account lockout notification to {Email}", user.Email);
                    }
                }

                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // Reset failed login counter on success
            user.FailedLoginCount = 0;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                action: "login_success",
                entityName: "User",
                entityId: user.UserId.ToString(),
                targetUserId: user.UserId,
                outcome: "success",
                details: new { email = user.Email, role = user.Role?.RoleCode, department = user.Department?.DepartmentName },
                screenCode: "login",
                ct: cancellationToken);

            var token = _jwtService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(8);

            return new LoginResponseDto
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                RoleName = user.Role?.RoleName ?? "User",
                RoleCode = user.Role?.RoleCode ?? "",
                DepartmentName = user.Department?.DepartmentName,
                AvatarInitials = user.AvatarInitials,
                AvatarColourHex = user.AvatarColourHex
            };
        }
    }
}
