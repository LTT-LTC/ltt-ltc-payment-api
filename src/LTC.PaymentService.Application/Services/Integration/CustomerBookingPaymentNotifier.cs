using System;
using System.Threading.Tasks;
using Grpc.Core;
using LTC.CustomerService.Grpc;
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
    private readonly BookingGrpc.BookingGrpcClient _grpcClient;
    private readonly PaymentCustomerIntegrationOptions _options;
    private readonly ILogger<CustomerBookingPaymentNotifier> _logger;

    public CustomerBookingPaymentNotifier(
        BookingGrpc.BookingGrpcClient grpcClient,
        IOptions<PaymentCustomerIntegrationOptions> options,
        ILogger<CustomerBookingPaymentNotifier> logger)
    {
        _grpcClient = grpcClient;
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

        _logger.LogInformation(
            "Notifying customer service via gRPC for booking {BookingId}, amount {Amount}, tenantId={TenantId}",
            bookingId, paidAmount, tenantId);

        var headers = new Metadata
        {
            { "x-internal-api-key", _options.CustomerServiceInternalApiKey ?? string.Empty },
        };

        if (tenantId.HasValue)
            headers.Add("__tenant", tenantId.Value.ToString());

        try
        {
            var request = new NotifyPaymentCompletedRequest
            {
                BookingId = bookingId.ToString(),
                PaidAmount = (double)paidAmount,
                Currency = currency,
                GatewayTransactionId = gatewayTransactionId ?? string.Empty,
                PaymentRequestId = paymentRequestId.ToString(),
                TenantId = tenantId?.ToString() ?? string.Empty,
            };

            var response = await _grpcClient.NotifyPaymentCompletedAsync(request, headers);

            _logger.LogInformation(
                "Customer booking gRPC notify succeeded for booking {BookingId}. Success={Success}, Message={Message}",
                bookingId, response.Success, response.Message);
        }
        catch (RpcException ex)
        {
            _logger.LogError(
                ex,
                "Customer booking gRPC notify failed for booking {BookingId}. Status={Status}, Detail={Detail}",
                bookingId, ex.StatusCode, ex.Status.Detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Customer booking gRPC notify unexpected error for booking {BookingId}",
                bookingId);
        }
    }
}
