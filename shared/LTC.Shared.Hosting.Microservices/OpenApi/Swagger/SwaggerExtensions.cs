using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Volo.Abp.Modularity;

namespace LTC.Shared.Hosting.Microservices.OpenApi.Swagger
{
    public static class SwaggerExtensions
    {
        public static void ConfigureSwaggerServices(this ServiceConfigurationContext context, string title, string version)
        {
            context.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                options.HideAbpEndpoints();
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                });
                options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer"), new List<string>()
                    }
                });
            });
        }

        public static void UseSwaggerUI(this IApplicationBuilder app, string name, string routePrefix)
        {
            app.UseSwagger(o =>
            {
                o.RouteTemplate = $"{routePrefix}/{{documentName}}/swagger.json";
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/{routePrefix}/v1/swagger.json", name);
                c.RoutePrefix = routePrefix;
            });
        }
    }
}
