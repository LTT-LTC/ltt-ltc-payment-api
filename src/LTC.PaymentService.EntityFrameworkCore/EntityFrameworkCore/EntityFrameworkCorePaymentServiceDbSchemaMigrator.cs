using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LTC.PaymentService.Data;
using Volo.Abp.DependencyInjection;

namespace LTC.PaymentService.EntityFrameworkCore;

public class EntityFrameworkCorePaymentServiceDbSchemaMigrator
    : IPaymentServiceDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCorePaymentServiceDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the PaymentServiceDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<PaymentServiceDbContext>()
            .Database
            .MigrateAsync();
    }
}
