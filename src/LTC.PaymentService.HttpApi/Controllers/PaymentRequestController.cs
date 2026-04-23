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
public class PaymentRequestController : AbpControllerBase
{
    private readonly IPaymentRequestAppService _appService;

    public PaymentRequestController(IPaymentRequestAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// Retrieve all payment request list (Admin/Manager only)
    /// </summary>
    [HttpGet("payment-request-all")]
    [ProducesResponseType(typeof(PagedResultDto<PaymentRequestOutputDto>), 200)]
    public async Task<IActionResult> GetPaymentRequestListAsync([FromQuery] GetPaymentRequestListInputDto input)
    {
        return Ok(await _appService.GetListAsync(input));
    }

    /// <summary>
    /// Retrieve specific payment request detail (Admin/Manager only)
    /// </summary>
    [HttpGet("payment-request/{id}")]
    [ProducesResponseType(typeof(PaymentRequestOutputDto), 200)]
    public async Task<IActionResult> GetPaymentRequestAsync(Guid id)
    {
        return Ok(await _appService.GetAsync(id));
    }
}
