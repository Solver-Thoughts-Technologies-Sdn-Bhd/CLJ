using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using XMLAutoTagger.Application.Common.Interfaces;

namespace XMLAutoTagger.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var id))
                {
                    return id;
                }

                // Fallback: extract from jwt_token cookie directly if available
                var token = _httpContextAccessor.HttpContext?.Request.Cookies["jwt_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(token);
                        var sub = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "nameid")?.Value;
                        if (!string.IsNullOrEmpty(sub) && Guid.TryParse(sub, out var guid))
                        {
                            return guid;
                        }
                    }
                    catch { }
                }

                return null;
            }
        }

        public string? Username
        {
            get
            {
                var name = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
                if (!string.IsNullOrEmpty(name)) return name;

                return _httpContextAccessor.HttpContext?.Request.Cookies["user_name"];
            }
        }

        public string? RoleCode
        {
            get
            {
                var code = _httpContextAccessor.HttpContext?.User?.FindFirstValue("role_code");
                if (!string.IsNullOrEmpty(code)) return code;

                var token = _httpContextAccessor.HttpContext?.Request.Cookies["jwt_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(token);
                        var rc = jwt.Claims.FirstOrDefault(c => c.Type == "role_code")?.Value;
                        if (!string.IsNullOrEmpty(rc)) return rc;
                    }
                    catch { }
                }

                var roleCookie = _httpContextAccessor.HttpContext?.Request.Cookies["user_role"];
                if (roleCookie != null)
                {
                    if (roleCookie.Equals("Administrator", StringComparison.OrdinalIgnoreCase)) return "admin";
                    if (roleCookie.IndexOf("mod", StringComparison.OrdinalIgnoreCase) >= 0) return "mod";
                }

                return null;
            }
        }

        public string? RoleName
        {
            get
            {
                var role = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
                if (!string.IsNullOrEmpty(role)) return role;

                return _httpContextAccessor.HttpContext?.Request.Cookies["user_role"];
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    return true;
                }

                return !string.IsNullOrEmpty(_httpContextAccessor.HttpContext?.Request.Cookies["jwt_token"]);
            }
        }

        public string? IpAddress
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null) return null;

                if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
                {
                    return forwarded.ToString().Split(',')[0].Trim();
                }

                return context.Connection.RemoteIpAddress?.ToString();
            }
        }

        public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
    }
}
