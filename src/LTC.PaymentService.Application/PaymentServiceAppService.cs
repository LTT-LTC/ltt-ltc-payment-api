using System;
using System.Collections.Generic;
using System.Text;
using LTC.PaymentService.Localization;
using Volo.Abp.Application.Services;

namespace LTC.PaymentService;

/* Inherit your application services from this class.
 */
public abstract class PaymentServiceAppService : ApplicationService
{
    protected PaymentServiceAppService()
    {
        LocalizationResource = typeof(PaymentServiceResource);
    }
}
