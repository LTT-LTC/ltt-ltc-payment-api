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
[Route("/ltc/payment-service/api")]
[Authorize(Roles = "Admin,Manager")]
public class RefundController : AbpControllerBase
{
    private readonly IRefundAppService _appService;

    public RefundController(IRefundAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// Retrieve all refund list (Admin/Manager only)
    /// </summary>
    [HttpGet("refund-all")]
    [ProducesResponseType(typeof(PagedResultDto<RefundOutputDto>), 200)]
    public async Task<IActionResult> GetRefundListAsync([FromQuery] GetRefundListInputDto input)
    {
        return Ok(await _appService.GetListAsync(input));
    }

    /// <summary>
    /// Retrieve specific refund detail (Admin/Manager only)
    /// </summary>
    [HttpGet("refund/{id}")]
    [ProducesResponseType(typeof(RefundOutputDto), 200)]
    public async Task<IActionResult> GetRefundAsync(Guid id)
    {
        return Ok(await _appService.GetAsync(id));
    }
}
