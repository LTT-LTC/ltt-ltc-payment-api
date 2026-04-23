using System.Collections.Generic;
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

                // Adds X-Tenant input box on every endpoint — guaranteed to be sent in curl
                options.OperationFilter<TenantHeaderOperationFilter>();

                // Bearer token global security
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description  = "JWT Authorization header. Format: 'Bearer {token}'",
                    Name         = "Authorization",
                    In           = ParameterLocation.Header,
                    Type         = SecuritySchemeType.Http,
                    Scheme       = "bearer",
                    BearerFormat = "JWT"
                });
                options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>() });
            });
        }

        public static void UseConfiguredSwagger(this IApplicationBuilder app, string name, string routePrefix)
        {
            app.UseSwagger(o =>
            {
                o.RouteTemplate = routePrefix + "/{documentName}/swagger.json";
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/{routePrefix}/v1/swagger.json", name);
                c.RoutePrefix = routePrefix;
            });
        }
    }
}

