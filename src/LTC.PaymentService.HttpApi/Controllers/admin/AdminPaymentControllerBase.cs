using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Admin
{
    /// <summary>
    /// Base controller for Admin-only operations in PaymentService.
    /// </summary>
    [RemoteService]
    [Area("admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public abstract class AdminPaymentControllerBase : AbpControllerBase
    {
    }
}
