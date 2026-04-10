using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Volo.Abp.MultiTenancy;

namespace LTC.Shared.Hosting.Microservices.MultiTenancy;

public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        var currentTenant = context.GetService<ICurrentTenant>();
        var tenantId = currentTenant?.Id;

        return new { Type = context.GetType(), DesignTime = designTime, TenantId = tenantId };
    }
}
