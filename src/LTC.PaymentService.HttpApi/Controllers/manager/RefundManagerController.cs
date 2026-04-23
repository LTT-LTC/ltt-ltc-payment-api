using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Manager;

[Route("/ltc/payment-service/manager/refund")]
public class RefundManagerController : RefundController
{
    public RefundManagerController(IRefundAppService appService) : base(appService)
    {
    }
}
