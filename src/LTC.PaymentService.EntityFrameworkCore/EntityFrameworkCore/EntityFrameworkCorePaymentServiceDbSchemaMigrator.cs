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
        var dbContext = _serviceProvider.GetRequiredService<PaymentServiceDbContext>();
        var schema = dbContext.GetCurrentSchema();

        if (!string.IsNullOrEmpty(schema) && schema != "dbo")
        {
            // Ensure the schema exists before migrating
            var sql = $"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{schema}') EXEC('CREATE SCHEMA [{schema}]')";
            await dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        await dbContext.Database.MigrateAsync();
    }
}
