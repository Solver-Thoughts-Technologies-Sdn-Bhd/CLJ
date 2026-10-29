using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XMLAutoTagger.Application.Common.Interfaces;
using XMLAutoTagger.Domain.Interfaces;
using XMLAutoTagger.Infrastructure.Persistence;
using XMLAutoTagger.Infrastructure.Services;

namespace XMLAutoTagger.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Database / Persistence
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                ));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IApplicationDbInitializer, ApplicationDbInitializer>();

            // 1. HttpContext Accessor (Required for CurrentUserService)
            services.AddHttpContextAccessor();

            // 2. JWT Bearer Authentication
            var jwtKey = configuration["Jwt:Key"];
            if (!string.IsNullOrEmpty(jwtKey))
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateIssuer = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = configuration["Jwt:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Cookies.TryGetValue("jwt_token", out var token) && !string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            }

            // 3. External Services
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddHttpClient();
            services.AddScoped<ICommunicationService, CommunicationService>();

            return services;
        }
    }
}
