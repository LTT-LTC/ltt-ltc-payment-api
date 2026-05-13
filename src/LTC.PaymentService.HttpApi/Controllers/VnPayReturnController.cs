using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
public class VnPayReturnController : AbpControllerBase
{
    private readonly IVnPayAppService _vnPayAppService;

    public VnPayReturnController(IVnPayAppService vnPayAppService)
    {
        _vnPayAppService = vnPayAppService;
    }

    /// <summary>
    /// Browser return URL: verifies signature, updates payment row when still pending, then redirects.
    /// </summary>
    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayReturnAsync()
    {
        var rawQuery = Request.QueryString.Value?.TrimStart('?') ?? string.Empty;
        var dict = rawQuery
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

        var url = await _vnPayAppService.ProcessReturnAsync(dict);
        return Redirect(url);
    }
}
