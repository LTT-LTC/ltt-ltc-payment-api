using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace LTC.Shared.Hosting.Microservices.Authentication
{
    public static class AuthenticationJwtBearerHandler
    {
        public static void ConfigureAuthenticationJwtBearer(this ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostingEnvironment = context.Services.GetHostingEnvironment();
            var securityKey = "AuthenticationJwtBearer:SecurityKey";
            var configuredSecurityKey = Environment.GetEnvironmentVariable(securityKey)
                ?? configuration[securityKey]
                ?? configuration["AuthServer:SecurityKey"];

            if (string.IsNullOrWhiteSpace(configuredSecurityKey))
            {
                if (hostingEnvironment.IsDevelopment())
                {
                    configuredSecurityKey = "LTC-Development-Only-Change-This-Security-Key-123456";
                }
                else
                {
                    throw new AbpException("AuthenticationJwtBearer:SecurityKey is missing.");
                }
            }

            var issuer = configuration["AuthenticationJwtBearer:Issuer"]
                ?? configuration["AuthServer:Authority"]
                ?? "https://localhost";

            var audience = configuration["AuthenticationJwtBearer:Audience"]
                ?? "CustomerService";

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuredSecurityKey));
            context.Services.Configure<TokenAuthOption>(options =>
            {
                options.SecurityKey = symmetricSecurityKey;
                options.Issuer = issuer;
                options.Audience = audience;
                options.SigningCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

                // Keep sane defaults when config keys are missing or invalid.
                var expiration = configuration.GetValue<int?>("AuthenticationJwtBearer:Expiration");
                var refreshExpiration = configuration.GetValue<int?>("AuthenticationJwtBearer:RefreshExpiration");

                options.Expiration = expiration.GetValueOrDefault(options.Expiration);
                options.RefreshExpiration = refreshExpiration.GetValueOrDefault(options.RefreshExpiration);

                if (options.Expiration <= 0)
                {
                    options.Expiration = 1;
                }

                if (options.RefreshExpiration <= 0)
                {
                    options.RefreshExpiration = 24 * 365;
                }
            });

            context.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Audience = audience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // The signing key must match!
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = symmetricSecurityKey,

                    // Relax validation for local development to avoid URL/Host mismatches
                    ValidateIssuer = false,
                    ValidateAudience = false,

                    // Fix ABP role mapping from generic Microsoft claim to ClaimTypes.Role
                    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                    NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",

                    // Validate the token expiry with a generous clock skew for local dev
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var claims = ctx.Principal?.Claims.ToList() ?? new List<Claim>();
                        var claimStrings = claims.Select(c => $"{c.Type}: {c.Value}");
                        
                        Log.Information("[JWT DEBUG] Token Validated Successfully for {User}", ctx.Principal?.Identity?.Name);
                        Log.Information("[JWT DEBUG] All Claims Found: \n{Claims}", string.Join("\n", claimStrings));
                        
                        // Map 'admin' to 'Admin' for case-sensitive ASP.NET identity checks, if needed,
                        // and ensure AbpClaimTypes are also populated if ABP is looking there.
                        if (ctx.Principal?.Identity is ClaimsIdentity identity)
                        {
                            var roleClaims = claims.Where(c => c.Type == identity.RoleClaimType).ToList();
                            foreach (var r in roleClaims)
                            {
                                if (r.Value == "admin")
                                {
                                    identity.AddClaim(new Claim(identity.RoleClaimType, "Admin"));
                                }
                            }
                        }

                        // Check specifically for Admin role presence
                        bool isAdmin = ctx.Principal?.IsInRole("Admin") ?? false;
                        bool hasAdminRoleClaim = claims.Any(c => c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase) && 
                            (c.Type == "role" || c.Type == ClaimTypes.Role || c.Type.EndsWith("/role")));
                        
                        Log.Information("[JWT DEBUG] Authorization Check - IsInRole('Admin'): {IsAdmin}, HasAdminRoleClaim: {HasAdminRoleClaim}", isAdmin, hasAdminRoleClaim);
                        
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = ctx =>
                    {
                        Log.Error(ctx.Exception, "[JWT DEBUG] Authentication Failed: {Message}", ctx.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}
