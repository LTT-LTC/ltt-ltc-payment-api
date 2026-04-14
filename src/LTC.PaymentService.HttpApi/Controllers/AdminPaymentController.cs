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
public class AdminPaymentController : AbpControllerBase
{
    private readonly IPaymentAppService _appService;

    public AdminPaymentController(IPaymentAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// Retrieve all payment list
    /// </summary>
    [HttpGet("payment-all")]
    [ProducesResponseType(typeof(PagedResultDto<PaymentOutputDto>), 200)]
    public async Task<IActionResult> GetPaymentListAsync([FromQuery] GetPaymentListInputDto input)
    {
        return Ok(await _appService.GetListAsync(input));
    }

    /// <summary>
    /// Retrieve specific payment detail
    /// </summary>
    [HttpGet("payment/{id}")]
    [ProducesResponseType(typeof(PaymentOutputDto), 200)]
    public async Task<IActionResult> GetPaymentAsync(Guid id)
    {
        return Ok(await _appService.GetAsync(id));
    }

    /// <summary>
    /// Retrieve audit logs for a payment 
    /// </summary>
    [HttpGet("payment/{id}/audit-log-all")]
    [ProducesResponseType(typeof(PagedResultDto<PaymentAuditLogOutputDto>), 200)]
    public async Task<IActionResult> GetAuditLogsAsync(Guid id, [FromQuery] PaginationInputDto input)
    {
        return Ok(await _appService.GetAuditLogsAsync(id, input));
    }
}
