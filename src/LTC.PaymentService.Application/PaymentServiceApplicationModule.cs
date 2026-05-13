using System;
using LTC.CustomerService.Grpc;
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
        var customerServiceBaseUrl = context.Services.GetConfiguration()["Integration:CustomerServiceBaseUrl"] ?? "http://ltt-ltc-customer-api:8080";
        context.Services.AddGrpcClient<BookingGrpc.BookingGrpcClient>(o =>
        {
            o.Address = new Uri(customerServiceBaseUrl);
        });
        context.Services.AddHttpClient(nameof(CustomerBookingSummaryClient))
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));
        context.Services.AddMapperlyObjectMapper<PaymentServiceApplicationModule>();
    }
}
