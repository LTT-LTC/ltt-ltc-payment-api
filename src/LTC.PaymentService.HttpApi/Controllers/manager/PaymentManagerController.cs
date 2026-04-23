using LTC.PaymentService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LTC.PaymentService.Controllers.Manager;

[Route("/ltc/payment-service/manager/payment")]
public class PaymentManagerController : PaymentController
{
    public PaymentManagerController(IPaymentAppService appService) : base(appService)
    {
    }
}
