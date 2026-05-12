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
        if (string.IsNullOrWhiteSpace(_options.CustomerServiceBaseUrl) ||
            string.IsNullOrWhiteSpace(_options.CustomerServiceInternalApiKey))
        {
            _logger.LogWarning("Customer booking notify skipped (Integration:CustomerServiceBaseUrl or ApiKey not set).");
            return;
        }

        var baseUrl = _options.CustomerServiceBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/ltc/customer-service/internal/booking/payment-completed";

        _logger.LogInformation("Notifying customer service of payment completion for booking {BookingId}, amount {Amount}", bookingId, paidAmount);

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(CustomerBookingPaymentNotifier));
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", _options.CustomerServiceInternalApiKey);
            if (tenantId.HasValue)
            {
                request.Headers.TryAddWithoutValidation("__tenant", tenantId.Value.ToString());
            }
            request.Content = JsonContent.Create(new
            {
                bookingId,
                paidAmount,
                currency,
                gatewayTransactionId,
                paymentRequestId
            });

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Customer booking notify failed for booking {BookingId}: {Status} {Body}",
                    bookingId,
                    (int)response.StatusCode,
                    body);
            }
            else
            {
                _logger.LogInformation("Customer booking notify succeeded for booking {BookingId}", bookingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer booking notify threw for booking {BookingId}", bookingId);
        }
    }
}
