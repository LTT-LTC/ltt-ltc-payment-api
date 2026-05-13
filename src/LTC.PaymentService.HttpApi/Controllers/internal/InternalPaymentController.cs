using System;
using System.Threading.Tasks;
using LTC.PaymentService.Interfaces;
using LTC.PaymentService.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Internal;

/// <summary>Service-to-service: allows customer-service to query payment status for a booking.</summary>
[Route("/ltc/payment-service/internal")]
[AllowAnonymous]
public class InternalPaymentController : AbpControllerBase
{
    private readonly IVnPayAppService _vnPayAppService;
    private readonly InternalApiOptions _internalApi;

    public InternalPaymentController(
        IVnPayAppService vnPayAppService,
        IOptions<InternalApiOptions> internalApi)
    {
        _vnPayAppService = vnPayAppService;
        _internalApi = internalApi.Value;
    }

    /// <summary>Returns the VNPAY payment status for a given booking.</summary>
    [HttpGet("payment-status/{bookingId:guid}")]
    public async Task<IActionResult> GetPaymentStatusByBookingAsync(
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
        Guid bookingId)
    {
        if (string.IsNullOrEmpty(_internalApi.CustomerServiceApiKey) ||
            !string.Equals(apiKey, _internalApi.CustomerServiceApiKey, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var result = await _vnPayAppService.GetPaymentStatusByBookingAsync(bookingId);

        if (result == null)
        {
            return NotFound(new { error = "No VNPAY payment record found for this booking." });
        }

        return Ok(result);
    }
}
