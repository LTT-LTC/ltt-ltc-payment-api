using Volo.Abp.Account;
using Volo.Abp.Mapperly;

using Volo.Abp.Identity;
using Volo.Abp.Modularity;


using Volo.Abp.TenantManagement;
using Microsoft.Extensions.DependencyInjection;

namespace LTC.PaymentService;

[DependsOn(
    typeof(PaymentServiceDomainModule),
    typeof(PaymentServiceApplicationContractsModule)
)]
public class PaymentServiceApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMapperlyObjectMapper<PaymentServiceApplicationModule>();
    }
}
