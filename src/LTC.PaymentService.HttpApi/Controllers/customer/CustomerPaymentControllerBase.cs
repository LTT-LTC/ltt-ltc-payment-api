using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Customer;

/// <summary>Base for customer routes: any authenticated principal (customer JWT).</summary>
[ApiController]
[Authorize]
public abstract class CustomerPaymentControllerBase : AbpControllerBase
{
}
