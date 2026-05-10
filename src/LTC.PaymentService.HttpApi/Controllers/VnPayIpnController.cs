using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers;

[RemoteService(Name = PaymentServiceRemoteServiceConsts.RemoteServiceName)]
[Area(PaymentServiceRemoteServiceConsts.ModuleName)]
[Route("/ltc/payment-service/api/payment")]
[Route("/payment-service/api/payment")]
public class VnPayIpnController : AbpControllerBase
{
    private readonly IVnPayAppService _vnPayAppService;

    public VnPayIpnController(IVnPayAppService vnPayAppService)
    {
        _vnPayAppService = vnPayAppService;
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
}
