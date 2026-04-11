using Localization.Resources.AbpUi;
using LTC.PaymentService.Localization;
using Volo.Abp.Account;

using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;


using Volo.Abp.TenantManagement;

namespace LTC.PaymentService;

[DependsOn(
    typeof(PaymentServiceApplicationContractsModule)
)]
public class PaymentServiceHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<PaymentServiceResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }
}
