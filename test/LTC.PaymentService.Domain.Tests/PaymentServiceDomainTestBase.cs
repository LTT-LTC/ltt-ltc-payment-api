using Volo.Abp.Modularity;

namespace LTC.PaymentService;

/* Inherit from this class for your domain layer tests. */
public abstract class PaymentServiceDomainTestBase<TStartupModule> : PaymentServiceTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
