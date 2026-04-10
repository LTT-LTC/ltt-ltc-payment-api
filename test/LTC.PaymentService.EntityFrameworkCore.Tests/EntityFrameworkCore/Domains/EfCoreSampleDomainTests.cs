using LTC.PaymentService.Samples;
using Xunit;

namespace LTC.PaymentService.EntityFrameworkCore.Domains;

[Collection(PaymentServiceTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<PaymentServiceEntityFrameworkCoreTestModule>
{

}
