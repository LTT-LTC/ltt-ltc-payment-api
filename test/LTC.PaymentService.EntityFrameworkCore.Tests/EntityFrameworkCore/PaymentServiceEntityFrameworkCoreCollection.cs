using Xunit;

namespace LTC.PaymentService.EntityFrameworkCore;

[CollectionDefinition(PaymentServiceTestConsts.CollectionDefinitionName)]
public class PaymentServiceEntityFrameworkCoreCollection : ICollectionFixture<PaymentServiceEntityFrameworkCoreFixture>
{

}
