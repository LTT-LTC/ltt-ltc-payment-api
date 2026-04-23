using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Admin;

[Route("/ltc/payment-service/admin/payment")]
public class PaymentAdminController : PaymentController
{
    public PaymentAdminController(IPaymentAppService appService) : base(appService)
    {
    }
}
