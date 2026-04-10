using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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
                    throw new InvalidOperationException(
                        "Missing JWT security key. Configure 'AuthenticationJwtBearer:SecurityKey' (or env var with same name)."
                    );
                }
            }

            var issuer = configuration["AuthenticationJwtBearer:Issuer"]
                ?? configuration["AuthServer:Authority"]
                ?? "https://localhost";

            var audience = configuration["AuthenticationJwtBearer:Audience"]
                ?? "AdministrationService";

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

                    // Validate the JWT Issuer (iss) claim
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    // Validate the JWT Audience (aud) claim
                    ValidateAudience = true,
                    ValidAudience = audience,

                    // Validate the token expiry
                    ValidateLifetime = true,

                    // If you want to allow a certain amount of clock drift, set that here
                    ClockSkew = TimeSpan.Zero
                };
            });
        }
    }
}
