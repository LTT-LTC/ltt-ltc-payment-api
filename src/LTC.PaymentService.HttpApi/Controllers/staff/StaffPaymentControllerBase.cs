using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Staff
{
    [RemoteService]
    [Area("staff")]
    [ApiController]
    [Authorize(Roles = "Staff")]
    public abstract class StaffPaymentControllerBase : AbpControllerBase
    {
    }
}
