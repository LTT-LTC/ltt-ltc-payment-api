using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.VirtualFileSystem;

namespace LTC.PaymentService;

[DependsOn(
    typeof(PaymentServiceApplicationContractsModule)
)]
public class PaymentServiceHttpApiClientModule : AbpModule
{
    public const string RemoteServiceName = "Default";

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(PaymentServiceApplicationContractsModule).Assembly,
            RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<PaymentServiceHttpApiClientModule>();
        });
    }
}


