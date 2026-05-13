using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LTC.PaymentService.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace LTC.PaymentService.Services.Integration;

public interface ICustomerBookingPaymentNotifier : ITransientDependency
{
    Task NotifyBookingPaidAsync(
        Guid bookingId,
        decimal paidAmount,
        string currency,
        string? gatewayTransactionId,
        Guid paymentRequestId,
        Guid? tenantId = null);
}

public class CustomerBookingPaymentNotifier : ICustomerBookingPaymentNotifier
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PaymentCustomerIntegrationOptions _options;
    private readonly ILogger<CustomerBookingPaymentNotifier> _logger;

    public CustomerBookingPaymentNotifier(
        IHttpClientFactory httpClientFactory,
        IOptions<PaymentCustomerIntegrationOptions> options,
        ILogger<CustomerBookingPaymentNotifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyBookingPaidAsync(
        Guid bookingId,
        decimal paidAmount,
        string currency,
        string? gatewayTransactionId,
        Guid paymentRequestId,
        Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(_options.CustomerServiceBaseUrl))
        {
            _logger.LogWarning("Customer booking notify skipped (Integration:CustomerServiceBaseUrl not set).");
            return;
        }

        var url = $"{_options.CustomerServiceBaseUrl.TrimEnd('/')}/ltc/customer-service/internal/booking/payment-completed";

        _logger.LogInformation(
            "Notifying customer service via HTTP for booking {BookingId}, amount {Amount}, tenantId={TenantId}",
            bookingId, paidAmount, tenantId);

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(CustomerBookingPaymentNotifier));

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", _options.CustomerServiceInternalApiKey ?? string.Empty);
            if (tenantId.HasValue)
                request.Headers.TryAddWithoutValidation("__tenant", tenantId.Value.ToString());

            request.Content = JsonContent.Create(new
            {
                bookingId,
                paidAmount,
                currency,
                gatewayTransactionId,
                paymentRequestId,
            });

            using var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Customer booking HTTP notify failed for booking {BookingId}. Status={Status}, Body={Body}",
                    bookingId, (int)response.StatusCode, body);
                return;
            }

            _logger.LogInformation(
                "Customer booking HTTP notify succeeded for booking {BookingId}.",
                bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Customer booking HTTP notify unexpected error for booking {BookingId}",
                bookingId);
        }
    }
}
