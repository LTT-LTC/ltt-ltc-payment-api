using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Interfaces;

namespace LTC.PaymentService.Controllers;

[RemoteService(Name = PaymentServiceRemoteServiceConsts.RemoteServiceName)]
[Area(PaymentServiceRemoteServiceConsts.ModuleName)]
[Route("/ltc/payment-service")]
[Authorize(Roles = "Admin,Manager")]
[Obsolete("Use role-specific endpoints under /admin or /manager. This route remains for compatibility.")]
public class PaymentController : AbpControllerBase
{
    private readonly IPaymentAppService _appService;

    public PaymentController(IPaymentAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// Retrieve all payment list (Admin/Manager only)
    /// </summary>
    [HttpGet("payment-all")]
    [ProducesResponseType(typeof(PagedResultDto<PaymentOutputDto>), 200)]
    public async Task<IActionResult> GetPaymentListAsync([FromQuery] GetPaymentListInputDto input)
    {
        return Ok(await _appService.GetPaymentListAsync(input));
    }

    /// <summary>
    /// Retrieve specific payment detail (Admin/Manager only)
    /// </summary>
    [HttpGet("payment/{id}")]
    [ProducesResponseType(typeof(PaymentOutputDto), 200)]
    public async Task<IActionResult> GetPaymentAsync(Guid id)
    {
        return Ok(await _appService.GetPaymentAsync(id));
    }

    /// <summary>
    /// Retrieve audit logs for a payment (Admin/Manager only)
    /// </summary>
    [HttpGet("payment/{id}/audit-log-all")]
    [ProducesResponseType(typeof(PagedResultDto<PaymentAuditLogOutputDto>), 200)]
    public async Task<IActionResult> GetAuditLogsAsync(Guid id, [FromQuery] PaginationInputDto input)
    {
        return Ok(await _appService.GetAuditLogsAsync(id, input));
    }
}
