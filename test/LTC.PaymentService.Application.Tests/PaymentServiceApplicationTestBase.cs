using Volo.Abp.Modularity;

namespace LTC.PaymentService;

public abstract class PaymentServiceApplicationTestBase<TStartupModule> : PaymentServiceTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
