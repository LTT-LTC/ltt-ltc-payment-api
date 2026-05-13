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

        _logger.LogInformation("Notifying customer service of payment completion for booking {BookingId}, amount {Amount}, tenantId={TenantId}",
            bookingId, paidAmount, tenantId);

        // Retry configuration: 3 attempts with exponential backoff (1s, 2s, 4s)
        const int maxRetries = 3;
        var retryDelays = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
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

                _logger.LogDebug("Attempt {Attempt}/{MaxRetries}: Sending notification to {Url} for booking {BookingId}",
                    attempt + 1, maxRetries, url, bookingId);

                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Customer booking notify succeeded for booking {BookingId} on attempt {Attempt}. Response: {Status} {Body}",
                        bookingId, attempt + 1, (int)response.StatusCode, body);
                    return; // Success - exit retry loop
                }
                else
                {
                    _logger.LogWarning(
                        "Customer booking notify failed for booking {BookingId} on attempt {Attempt}: {Status} {Body}. Retrying...",
                        bookingId, attempt + 1, (int)response.StatusCode, body);

                    // Don't retry on 4xx errors (client errors)
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        _logger.LogError(
                            "Customer booking notify failed with client error for booking {BookingId}: {Status} {Body}. Not retrying.",
                            bookingId, (int)response.StatusCode, body);
                        return;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    "Customer booking notify HTTP error for booking {BookingId} on attempt {Attempt}: {Message}. Retrying...",
                    bookingId, attempt + 1, ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(
                    "Customer booking notify timeout for booking {BookingId} on attempt {Attempt}: {Message}. Retrying...",
                    bookingId, attempt + 1, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Customer booking notify unexpected error for booking {BookingId} on attempt {Attempt}",
                    bookingId, attempt + 1);
            }

            // Wait before next retry (except on last attempt)
            if (attempt < maxRetries - 1)
            {
                _logger.LogInformation(
                    "Waiting {DelayMs}ms before retry {NextAttempt}/{MaxRetries} for booking {BookingId}",
                    retryDelays[attempt].TotalMilliseconds, attempt + 2, maxRetries, bookingId);
                await Task.Delay(retryDelays[attempt]);
            }
        }

        _logger.LogError(
            "Customer booking notify failed permanently after {MaxRetries} attempts for booking {BookingId}. " +
            "Booking status and member card points may not be updated. Manual intervention required.",
            maxRetries, bookingId);
    }
}
