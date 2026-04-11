using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;

namespace LTC.PaymentService;

[DependsOn(
    typeof(PaymentServiceDomainSharedModule),
    typeof(AbpObjectExtendingModule)
)]
public class PaymentServiceApplicationContractsModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PaymentServiceDtoExtensions.Configure();
    }
}
