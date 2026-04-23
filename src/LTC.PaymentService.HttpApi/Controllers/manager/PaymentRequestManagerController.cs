using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Manager;

[Route("/ltc/payment-service/manager/payment-request")]
public class PaymentRequestManagerController : PaymentRequestController
{
    public PaymentRequestManagerController(IPaymentRequestAppService appService) : base(appService)
    {
    }
}
