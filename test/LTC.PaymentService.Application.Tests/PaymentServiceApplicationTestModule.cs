using Volo.Abp.Modularity;

namespace LTC.PaymentService;

[DependsOn(
    typeof(PaymentServiceApplicationModule),
    typeof(PaymentServiceDomainTestModule)
)]
public class PaymentServiceApplicationTestModule : AbpModule
{

}
