using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Interfaces;

namespace LTC.PaymentService.Controllers;

[RemoteService(Name = PaymentServiceRemoteServiceConsts.RemoteServiceName)]
[Area(PaymentServiceRemoteServiceConsts.ModuleName)]
[Route("/ltc/payment-service/api/payment")]
[Route("/payment-service/api/payment")]
public class VnPayController : AbpControllerBase
{
    private readonly IVnPayAppService _vnPayAppService;

    public VnPayController(IVnPayAppService vnPayAppService)
    {
        _vnPayAppService = vnPayAppService;
    }

    /// <summary>
    /// Creates a signed VNPAY payment URL (authenticated booking/customer flow).
    /// </summary>
    [HttpPost("create-payment-url")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentUrlAsync([FromBody] CreateVnPayPaymentUrlInputDto input)
    {
        var ip = GetClientIp(HttpContext);
        return Ok(await _vnPayAppService.CreatePaymentUrlAsync(input, ip));
    }

    /// <summary>
    /// VNPAY server IPN (Instant Payment Notification). Always returns HTTP 200 with JSON body.
    /// </summary>
    [HttpGet("vnpay-ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayIpnAsync()
    {
        var dict = Request.Query.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.FirstOrDefault() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        var result = await _vnPayAppService.ProcessIpnAsync(dict);
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        });

        return Content(json, "application/json; charset=utf-8");
    }

    /// <summary>
    /// Browser return URL: verifies signature and redirects to configured frontend URLs (no payment DB updates).
    /// </summary>
    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayReturnAsync()
    {
        var dict = Request.Query.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.FirstOrDefault() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        var url = await _vnPayAppService.ProcessReturnAsync(dict);
        return Redirect(url);
    }

    private static string GetClientIp(HttpContext httpContext)
    {
        var fwd = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
            return fwd.Split(',')[0].Trim();

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}
