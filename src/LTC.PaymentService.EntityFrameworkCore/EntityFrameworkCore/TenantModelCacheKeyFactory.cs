using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LTC.PaymentService.EntityFrameworkCore;

public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is PaymentServiceDbContext tenantContext)
        {
            return (context.GetType(), tenantContext.GetCurrentSchema(), designTime);
        }

        return (context.GetType(), designTime);
    }
}
