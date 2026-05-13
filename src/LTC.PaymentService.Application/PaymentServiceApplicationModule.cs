using System;
using LTC.PaymentService.Options;
using LTC.PaymentService.Services.Integration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.Mapperly;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;

namespace LTC.PaymentService;

[DependsOn(
    typeof(PaymentServiceDomainModule),
    typeof(PaymentServiceApplicationContractsModule)
)]
public class PaymentServiceApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<VnPayOptions>(context.Services.GetConfiguration().GetSection(VnPayOptions.SectionName));
        context.Services.Configure<InternalApiOptions>(context.Services.GetConfiguration().GetSection(InternalApiOptions.SectionName));
        context.Services.Configure<PaymentCustomerIntegrationOptions>(
            context.Services.GetConfiguration().GetSection(PaymentCustomerIntegrationOptions.SectionName));
        context.Services.AddHttpClient(nameof(CustomerBookingPaymentNotifier))
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));
        context.Services.AddHttpClient(nameof(CustomerBookingSummaryClient))
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));
        context.Services.AddMapperlyObjectMapper<PaymentServiceApplicationModule>();
    }
}
