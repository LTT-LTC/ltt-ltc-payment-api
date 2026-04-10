using LTC.PaymentService.Samples;
using Xunit;

namespace LTC.PaymentService.EntityFrameworkCore.Applications;

[Collection(PaymentServiceTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<PaymentServiceEntityFrameworkCoreTestModule>
{

}
