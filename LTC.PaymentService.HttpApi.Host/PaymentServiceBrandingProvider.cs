using Microsoft.Extensions.Localization;
using LTC.PaymentService.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace LTC.PaymentService;

[Dependency(ReplaceServices = true)]
public class PaymentServiceBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<PaymentServiceResource> _localizer;

    public PaymentServiceBrandingProvider(IStringLocalizer<PaymentServiceResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
