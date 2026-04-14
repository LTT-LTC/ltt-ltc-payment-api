using System;
using System.Threading.Tasks;
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
[Route("api/administration/admin")]
public class AdminRefundController : AbpControllerBase
{
    private readonly IRefundAppService _appService;

    public AdminRefundController(IRefundAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// Retrieve all refund list
    /// </summary>
    [HttpGet("refund-all")]
    [ProducesResponseType(typeof(PagedResultDto<RefundOutputDto>), 200)]
    public async Task<IActionResult> GetRefundListAsync([FromQuery] GetRefundListInputDto input)
    {
        return Ok(await _appService.GetListAsync(input));
    }

    /// <summary>
    /// Retrieve specific refund
    /// </summary>
    [HttpGet("refund/{id}")]
    [ProducesResponseType(typeof(RefundOutputDto), 200)]
    public async Task<IActionResult> GetRefundAsync(Guid id)
    {
        return Ok(await _appService.GetAsync(id));
    }

    /// <summary>
    /// Initiate a new refund
    /// </summary>
    [HttpPost("refund")]
    [ProducesResponseType(typeof(RefundOutputDto), 200)]
    public async Task<IActionResult> CreateRefundAsync([FromBody] CreateRefundInputDto input)
    {
        return Ok(await _appService.InitiateRefundAsync(input));
    }
}
