using Volo.Abp.Settings;

namespace LTC.PaymentService.Settings;

public class PaymentServiceSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(PaymentServiceSettings.MySetting1));
    }
}
