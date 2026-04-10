using LTC.PaymentService.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace LTC.PaymentService.Permissions;

public class PaymentServicePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(PaymentServicePermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(PaymentServicePermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PaymentServiceResource>(name);
    }
}
