using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using System.Threading.Tasks;

namespace LTC.Shared.Hosting.Microservices.MultiTenancy
{
    public class TenantValidationFilter : IAsyncActionFilter, ITransientDependency
    {
        private readonly ICurrentTenant _currentTenant;

        public TenantValidationFilter(ICurrentTenant currentTenant)
        {
            _currentTenant = currentTenant;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            if (request.Headers.TryGetValue("X-Tenant", out var tenantHeader))
            {
                var tenantName = tenantHeader.ToString();
                if (!string.IsNullOrWhiteSpace(tenantName) && _currentTenant.Id == null)
                {
                    throw new UserFriendlyException($"Tenant '{tenantName}' is invalid or could not be found.");
                }
            }

            await next();
        }
    }
}
