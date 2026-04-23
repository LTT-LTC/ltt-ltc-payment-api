using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Admin;

[Route("/ltc/payment-service/admin/refund")]
public class RefundAdminController : RefundController
{
    public RefundAdminController(IRefundAppService appService) : base(appService)
    {
    }
}
