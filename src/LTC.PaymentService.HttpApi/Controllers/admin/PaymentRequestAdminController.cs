using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Admin;

[Route("/ltc/payment-service/admin/payment-request")]
public class PaymentRequestAdminController : PaymentRequestController
{
    public PaymentRequestAdminController(IPaymentRequestAppService appService) : base(appService)
    {
    }
}
