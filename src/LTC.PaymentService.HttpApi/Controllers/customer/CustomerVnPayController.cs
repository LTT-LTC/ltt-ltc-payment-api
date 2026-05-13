using System;
using System.Linq;
using System.Threading.Tasks;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Customer;

[RemoteService(Name = PaymentServiceRemoteServiceConsts.RemoteServiceName)]
[Area(PaymentServiceRemoteServiceConsts.ModuleName)]
[Route("/ltc/payment-service/customer/payment")]
[Route("/payment-service/customer/payment")]
public class CustomerVnPayController : CustomerPaymentControllerBase
{
    private readonly IVnPayAppService _vnPayAppService;

    public CustomerVnPayController(IVnPayAppService vnPayAppService)
    {
        _vnPayAppService = vnPayAppService;
    }

    /// <summary>
    /// Creates a signed VNPAY payment URL for customer checkout.
    /// </summary>
    [HttpPost("create-payment-url")]
    [Consumes("application/json")]
    public async Task<IActionResult> CreatePaymentUrlAsync([FromBody] CreateVnPayPaymentUrlInputDto? input)
    {
        if (input is null)
        {
            throw new UserFriendlyException(
                "Request body is required. Send JSON with bookingId (GUID), amount (VND, greater than 0), and orderInfo (string). Example: {\"bookingId\":\"...\",\"amount\":100000,\"orderInfo\":\"Tickets\"}");
        }

        var ip = GetClientIp(HttpContext);
        return Ok(await _vnPayAppService.CreatePaymentUrlAsync(input, ip));
    }

    private static string GetClientIp(HttpContext httpContext)
    {
        var fwd = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
            return fwd.Split(',')[0].Trim();

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    /// <summary>
    /// Manually check and complete a VNPay payment by booking ID.
    /// This is a fallback endpoint for when IPN/browser return fail to update booking status.
    /// </summary>
    [HttpPost("manual-complete/{bookingId:guid}")]
    [ProducesResponseType(typeof(ManualVnpayCompletionOutputDto), 200)]
    public async Task<IActionResult> ManualCompleteAsync(Guid bookingId)
    {
        var result = await _vnPayAppService.ManualCompleteByBookingAsync(bookingId);
        return Ok(result);
    }
}
