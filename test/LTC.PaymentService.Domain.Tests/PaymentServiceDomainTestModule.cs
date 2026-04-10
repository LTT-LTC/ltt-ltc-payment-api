using Volo.Abp.Modularity;

namespace LTC.PaymentService;

[DependsOn(
    typeof(PaymentServiceDomainModule),
    typeof(PaymentServiceTestBaseModule)
)]
public class PaymentServiceDomainTestModule : AbpModule
{

}
