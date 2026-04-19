using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LTC.Shared.Hosting.Microservices.OpenApi.Swagger
{
    public class TenantHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<IOpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name        = "X-Tenant",
                In          = ParameterLocation.Header,
                Required    = false,
                Description = "Tenant name (schema). Example: LTC",
                Schema      = new OpenApiSchema { Type = JsonSchemaType.String }
            });
        }
    }
}
