using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Interfaces;

namespace LTC.PaymentService.Controllers.Customer;

/// <summary>
/// Customer-facing payment APIs: authenticated users only; data scoped to JWT user id and tenant.
/// Do not use legacy <c>/payment-all</c> — that route is Admin/Manager only.
/// </summary>
[RemoteService(Name = PaymentServiceRemoteServiceConsts.RemoteServiceName)]
[Area(PaymentServiceRemoteServiceConsts.ModuleName)]
[Route("/ltc/payment-service/customer/payment")]
public class CustomerPaymentController : CustomerPaymentControllerBase
{
    private readonly IPaymentAppService _appService;

    public CustomerPaymentController(IPaymentAppService appService)
    {
        _appService = appService;
    }

    /// <summary>Paged payment history for the logged-in customer.</summary>
    [HttpGet("my-payments")]
    [ProducesResponseType(typeof(PagedResultDto<PaymentOutputDto>), 200)]
    public async Task<IActionResult> GetMyPaymentsAsync([FromQuery] GetPaymentListInputDto input)
    {
        return Ok(await _appService.GetMyPaymentsAsync(input));
    }

    /// <summary>Payment detail if it belongs to the logged-in customer.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentOutputDto), 200)]
    public async Task<IActionResult> GetMyPaymentAsync(Guid id)
    {
        return Ok(await _appService.GetMyPaymentAsync(id));
    }
}
