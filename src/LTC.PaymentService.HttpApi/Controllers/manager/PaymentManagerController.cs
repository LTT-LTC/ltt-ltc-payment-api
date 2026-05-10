using System.Threading.Tasks;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Manager;

[Route("/ltc/payment-service/manager/payment")]
[Authorize(Roles = "Admin,Manager")]
public class PaymentManagerController : PaymentController
{
    private readonly IPaymentAppService _dashboardService;

    public PaymentManagerController(IPaymentAppService appService) : base(appService)
    {
        _dashboardService = appService;
    }

    [HttpGet("dashboard-summary")]
    [ProducesResponseType(typeof(DashboardSummaryOutputDto), 200)]
    public async Task<IActionResult> GetDashboardSummaryAsync([FromQuery] DashboardSummaryInputDto input)
    {
        return Ok(await _dashboardService.GetDashboardSummaryAsync(input));
    }
}
