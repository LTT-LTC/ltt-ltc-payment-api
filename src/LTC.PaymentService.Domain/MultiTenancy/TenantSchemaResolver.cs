using Microsoft.AspNetCore.Http;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace LTC.PaymentService.MultiTenancy;

public class TenantSchemaResolver : ITenantSchemaResolver, ITransientDependency
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantSchemaResolver(
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor)
    {
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetSchemaName()
    {
        // 1. Prefer ABP-resolved tenant name (from AbpTenants table)
        if (!string.IsNullOrWhiteSpace(_currentTenant.Name))
            return _currentTenant.Name;

        // 2. Fall back to raw X-Tenant header value
        //    This allows schema routing even when tenant is not in AbpTenants store.
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var tenantKey = "X-Tenant";
            if (httpContext.Request.Headers.TryGetValue(tenantKey, out var headerValue))
            {
                var tenantName = headerValue.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(tenantName))
                    return tenantName;
            }
        }

        // 3. Default schema
        return "dbo";
    }
}

public interface ITenantSchemaResolver
{
    string GetSchemaName();
}
