using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Uow;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;

using Volo.Abp.Modularity;





namespace LTC.PaymentService.EntityFrameworkCore;

[DependsOn(
    typeof(PaymentServiceDomainModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule),
    typeof(AbpAuditLoggingEntityFrameworkCoreModule),
    typeof(Volo.Abp.TenantManagement.EntityFrameworkCore.AbpTenantManagementEntityFrameworkCoreModule)
)]
public class PaymentServiceEntityFrameworkCoreModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PaymentServiceEfCoreEntityExtensionMappings.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<PaymentServiceDbContext>(options =>
        {
                /* Remove "includeAllEntities: true" to create
                 * default repositories only for aggregate roots */
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        context.Services.Replace(ServiceDescriptor.Singleton<IModelCacheKeyFactory, TenantModelCacheKeyFactory>());

        Configure<AbpDbContextOptions>(options =>
        {
                /* The main point to change your DBMS.
                 * See also PaymentServiceMigrationsDbContextFactory for EF Core tooling. */
            options.UseSqlServer();
        });

    }
}
