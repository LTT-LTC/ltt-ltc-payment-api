using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Manager
{
    /// <summary>
    /// Base controller for Manager operations in PaymentService.
    /// Managers have read access and limited write access per entity policy.
    /// </summary>
    [RemoteService]
    [Area("manager")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public abstract class ManagerPaymentControllerBase : AbpControllerBase
    {
    }
}
