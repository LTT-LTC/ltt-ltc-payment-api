using System.Linq;
using System.Threading.Tasks;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Admin;

[RemoteService(Name = PaymentServiceRemoteServiceConsts.RemoteServiceName)]
[Area(PaymentServiceRemoteServiceConsts.ModuleName)]
[Route("/ltc/payment-service/admin/payment")]
[Route("/payment-service/admin/payment")]
public class AdminVnPayController : AdminPaymentControllerBase
{
    private readonly IVnPayAppService _vnPayAppService;

    public AdminVnPayController(IVnPayAppService vnPayAppService)
    {
        _vnPayAppService = vnPayAppService;
    }

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
}
