using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Entities;
using LTC.PaymentService.Interfaces;
using LTC.PaymentService.MultiTenancy;
using LTC.PaymentService.Options;
using LTC.PaymentService.Services.Integration;
using LTC.PaymentService.VnPay;
using LTC.Shared.Hosting.Microservices.Timing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using System.Linq;

namespace LTC.PaymentService.Services;

/// <remarks>
/// Dynamic API controller generation is disabled; HTTP routes are defined on the explicit VnPay API controller.
/// </remarks>
[RemoteService(IsEnabled = false)]
public class VnPayAppService : ApplicationService, IVnPayAppService
{
    private readonly VnPayOptions _options;
    private readonly IGmt7Clock _gmt7Clock;
    private readonly IRepository<PaymentRequest, Guid> _paymentRequestRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<PaymentAuditLog, Guid> _paymentAuditLogRepository;
    private readonly IRepository<VnpayTxnRouting, Guid> _vnpayTxnRoutingRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICustomerBookingPaymentNotifier _customerBookingPaymentNotifier;
    private readonly ILogger<VnPayAppService> _logger;

    public VnPayAppService(
        IOptions<VnPayOptions> options,
        IGmt7Clock gmt7Clock,
        IRepository<PaymentRequest, Guid> paymentRequestRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<PaymentAuditLog, Guid> paymentAuditLogRepository,
        IRepository<VnpayTxnRouting, Guid> vnpayTxnRoutingRepository,
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor,
        ICustomerBookingPaymentNotifier customerBookingPaymentNotifier,
        ILogger<VnPayAppService> logger)
    {
        _options = options.Value;
        _gmt7Clock = gmt7Clock;
        _paymentRequestRepository = paymentRequestRepository;
        _paymentRepository = paymentRepository;
        _paymentAuditLogRepository = paymentAuditLogRepository;
        _vnpayTxnRoutingRepository = vnpayTxnRoutingRepository;
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
        _customerBookingPaymentNotifier = customerBookingPaymentNotifier;
        _logger = logger;
    }

    public async Task<CreateVnPayPaymentUrlOutputDto> CreatePaymentUrlAsync(
        CreateVnPayPaymentUrlInputDto input,
        string clientIpAddress)
    {
        if (MultiTenancyConsts.IsEnabled && !CurrentTenant.Id.HasValue)
            throw new UserFriendlyException("Tenant context is required for payment.");

        if (CurrentUser.Id == null)
            throw new UserFriendlyException("Authentication required.");

        if (string.IsNullOrWhiteSpace(_options.TmnCode) || string.IsNullOrWhiteSpace(_options.HashSecret))
            throw new UserFriendlyException("VNPAY is not configured (TmnCode / HashSecret).");

        if (string.IsNullOrWhiteSpace(_options.PaymentUrl))
            throw new UserFriendlyException("VNPAY PaymentUrl is not configured.");

        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            throw new UserFriendlyException("VNPAY PublicBaseUrl is not configured.");

        if (input.Amount <= 0)
            throw new UserFriendlyException("Amount must be greater than zero.");

        var now = _gmt7Clock.Gmt7Now;

        var returnFullUrl = CombineUrl(_options.PublicBaseUrl, _options.ReturnPath);
        var ipnFullUrl = CombineUrl(_options.PublicBaseUrl, _options.IpnPath);

        var paymentRequest = new PaymentRequest
        {
            TenantId = CurrentTenant.Id,
            BookingId = input.BookingId,
            CustomerId = CurrentUser.Id.Value,
            Amount = input.Amount,
            Currency = "VND",
            PaymentGateway = "VNPAY",
            GatewayOrderId = null,
            ReturnUrl = ResolveFrontendOrigin(),
            NotifyUrl = null,
            CreatedAt = now,
            ExpiredAt = now.AddMinutes(Math.Max(1, _options.OrderExpireMinutes))
        };

        await _paymentRequestRepository.InsertAsync(paymentRequest, autoSave: true);

        var txnRef = paymentRequest.Id.ToString();
        paymentRequest.GatewayOrderId = txnRef;
        await _paymentRequestRepository.UpdateAsync(paymentRequest, autoSave: true);

        var payment = new Payment
        {
            TenantId = CurrentTenant.Id,
            PaymentRequestId = paymentRequest.Id,
            BookingId = paymentRequest.BookingId,
            Amount = paymentRequest.Amount,
            PaymentMethod = "VNPAY",
            PaymentStatus = "PENDING"
        };

        await _paymentRepository.InsertAsync(payment, autoSave: true);

        if (MultiTenancyConsts.IsEnabled)
        {
            try
            {
                await _vnpayTxnRoutingRepository.InsertAsync(new VnpayTxnRouting(paymentRequest.Id)
                {
                    TenantId = CurrentTenant.Id!.Value,
                    TenantName = ResolveTenantNameForVnpayRouting()
                }, autoSave: true);
            }
            catch (Exception ex) when (IsMissingVnpayRoutingTable(ex))
            {
                throw new UserFriendlyException(
                    "Payment routing table is missing: dbo.VnpayTxnRoutings. Run migrations (or create the table) on payment DB before creating VNPAY URLs.");
            }
        }

        var locale = string.IsNullOrWhiteSpace(input.Locale) ? "vn" : input.Locale!;
        var orderInfo = TruncateOrderInfo(input.OrderInfo);

        var requestData = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_Amount"] = VnPayLibrary.ToVnpAmountMinor(input.Amount).ToString(CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = txnRef,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = locale,
            ["vnp_ReturnUrl"] = returnFullUrl,
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(clientIpAddress) ? "127.0.0.1" : clientIpAddress,
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
        };

        if (_options.IncludeIpnUrlInPaymentRequest)
            requestData["vnp_IpnUrl"] = ipnFullUrl;

        var bankCode = input.BankCode?.Trim();
        if (!string.IsNullOrEmpty(bankCode))
            requestData["vnp_BankCode"] = bankCode;

        var paymentUrl = VnPayLibrary.BuildPaymentRedirectUrl(_options.PaymentUrl, requestData, _options.HashSecret);

        return new CreateVnPayPaymentUrlOutputDto { PaymentUrl = paymentUrl };
    }

    public async Task<VnPayIpnResponseDto> ProcessIpnAsync(IReadOnlyDictionary<string, string> queryParameters)
    {
        Guid paymentRequestId = Guid.Empty;

        try
        {
            var query = NormalizeQuery(queryParameters);

            if (!VnPayLibrary.HasAnyVnpParameter(query))
            {
                _logger.LogWarning("VNPay IPN: No VNP parameters found in request");
                return Rsp("99", "Input data required");
            }

            if (!VnPayLibrary.ValidateSignature(query, _options.HashSecret, out var receivedHash))
            {
                // Log detailed signature debugging information
                var signData = VnPayLibrary.BuildSignData(query);
                var computedHash = VnPayLibrary.HmacSha512Hex(_options.HashSecret, signData);
                _logger.LogError(
                    "VNPay IPN: Invalid signature. ReceivedHash={ReceivedHash}, ComputedHash={ComputedHash}, SignData={SignData}, QueryParams={QueryParams}",
                    receivedHash,
                    computedHash,
                    signData,
                    string.Join(", ", query.Select(kv => $"{kv.Key}={kv.Value}")));
                return Rsp("97", "Invalid Signature");
            }
            _logger.LogInformation("VNPay IPN: Signature validated successfully");

            if (!query.TryGetValue("vnp_TxnRef", out var txnRef) || !Guid.TryParse(txnRef, out paymentRequestId))
            {
                _logger.LogWarning("VNPay IPN: Invalid or missing vnp_TxnRef");
                return Rsp("01", "Order not found");
            }

            if (!query.TryGetValue("vnp_Amount", out var vnpAmountStr))
            {
                _logger.LogWarning("VNPay IPN: Missing vnp_Amount for payment request {PaymentRequestId}", paymentRequestId);
                return Rsp("04", "Invalid amount");
            }

            if (!VnPayLibrary.TryParseVnpAmountMajor(vnpAmountStr, out var vnpAmountMajor))
            {
                _logger.LogWarning("VNPay IPN: Invalid vnp_Amount format for payment request {PaymentRequestId}", paymentRequestId);
                return Rsp("04", "Invalid amount");
            }

            _logger.LogInformation("VNPay IPN: Received for payment request {PaymentRequestId} with amount {Amount}", paymentRequestId, vnpAmountMajor);

            IDisposable? tenantScope = null;
            try
            {
                if (MultiTenancyConsts.IsEnabled)
                {
                    var routing = await _vnpayTxnRoutingRepository.FindAsync(paymentRequestId);
                    if (routing == null)
                        return Rsp("01", "Order not found");

                    tenantScope = _currentTenant.Change(routing.TenantId, routing.TenantName);
                }

                var paymentRequest = await _paymentRequestRepository.FirstOrDefaultAsync(x => x.Id == paymentRequestId);
                if (paymentRequest == null)
                {
                    _logger.LogWarning("VNPay IPN: Payment request {PaymentRequestId} not found", paymentRequestId);
                    return Rsp("01", "Order not found");
                }

                var payment = await _paymentRepository.FirstOrDefaultAsync(x => x.PaymentRequestId == paymentRequest.Id);
                if (payment == null)
                {
                    _logger.LogWarning("VNPay IPN: Payment not found for payment request {PaymentRequestId}", paymentRequestId);
                    return Rsp("01", "Order not found");
                }

                if (vnpAmountMajor != paymentRequest.Amount)
                {
                    _logger.LogWarning("VNPay IPN: Amount mismatch for payment request {PaymentRequestId}. Expected {Expected}, got {Actual}", paymentRequestId, paymentRequest.Amount, vnpAmountMajor);
                    return Rsp("04", "Invalid amount");
                }

                if (payment.PaymentStatus == "SUCCESS")
                {
                    _logger.LogInformation("VNPay IPN: Payment {PaymentRequestId} already confirmed, skipping", paymentRequestId);
                    return Rsp("02", "Order already confirmed");
                }

                if (payment.PaymentStatus == "FAILED")
                {
                    _logger.LogInformation("VNPay IPN: Payment {PaymentRequestId} was FAILED, confirming success", paymentRequestId);
                    return Rsp("00", "Confirm success");
                }

                _logger.LogInformation("VNPay IPN: Processing payment callback for payment request {PaymentRequestId}, booking {BookingId}", paymentRequestId, paymentRequest.BookingId);
                await ApplyVnpGatewayCallbackAsync(paymentRequest, payment, query, auditEventType: "VnpayIpn");
                _logger.LogInformation("VNPay IPN: Successfully processed payment callback for payment request {PaymentRequestId}", paymentRequestId);

                return Rsp("00", "Confirm success");
            }
            finally
            {
                tenantScope?.Dispose();
            }
        }
        catch (Exception ex)
        {
            // VNPAY expects 200 + JSON response body, not server 500.
            _logger.LogError(ex, "VNPay IPN: Unhandled error processing payment request {PaymentRequestId}", paymentRequestId);
            return Rsp("99", "Unknown error");
        }
    }

    public async Task<string> ProcessReturnAsync(IReadOnlyDictionary<string, string> queryParameters)
    {
        try
        {
            var query = NormalizeQuery(queryParameters);

            if (!VnPayLibrary.ValidateSignature(query, _options.HashSecret, out var returnHash))
            {
                // Log detailed signature debugging information for return URL
                var signData = VnPayLibrary.BuildSignData(query);
                var computedHash = VnPayLibrary.HmacSha512Hex(_options.HashSecret, signData);
                _logger.LogError(
                    "VNPay Return: Invalid signature. ReceivedHash={ReceivedHash}, ComputedHash={ComputedHash}, SignData={SignData}, QueryParams={QueryParams}",
                    returnHash,
                    computedHash,
                    signData,
                    string.Join(", ", query.Select(kv => $"{kv.Key}={kv.Value}")));
                return _options.FrontendFailureUrl;
            }
            _logger.LogInformation("VNPay Return: Signature validated successfully");

            // Same callback params as IPN: persist SUCCESS/FAILED when IPN never reaches this host (localhost, firewall).
            await TryFinalizePaymentFromBrowserReturnAsync(query);

            var responseCode = query.GetValueOrDefault("vnp_ResponseCode");
            var transactionStatus = query.GetValueOrDefault("vnp_TransactionStatus");
            var success = responseCode == "00" && transactionStatus == "00";

            if (!Guid.TryParse(query.GetValueOrDefault("vnp_TxnRef"), out var paymentRequestId))
                return success ? _options.FrontendSuccessUrl : _options.FrontendFailureUrl;

            var returnContext = await TryResolveReturnContextAsync(paymentRequestId);
            var bookingId = returnContext.BookingId;

            if (bookingId.HasValue && !string.IsNullOrWhiteSpace(returnContext.FrontendOrigin))
            {
                var targetPath = success
                    ? $"/booking/{bookingId.Value:D}/processing?vnpay=1&status=success"
                    : $"/booking/{bookingId.Value:D}/processing?vnpay=1&status=failed";
                return CombineUrl(returnContext.FrontendOrigin, targetPath);
            }

            var baseUrl = success ? _options.FrontendSuccessUrl : _options.FrontendFailureUrl;
            return AppendBookingIdQuery(baseUrl, bookingId);
        }
        catch
        {
            // Never fail hard on browser return; always redirect user to failure flow.
            return _options.FrontendFailureUrl;
        }
    }

    private async Task<(Guid? BookingId, string? FrontendOrigin)> TryResolveReturnContextAsync(Guid paymentRequestId)
    {
        IDisposable? tenantScope = null;
        try
        {
            if (MultiTenancyConsts.IsEnabled)
            {
                var routing = await _vnpayTxnRoutingRepository.FindAsync(paymentRequestId);
                if (routing == null)
                    return (null, null);

                tenantScope = _currentTenant.Change(routing.TenantId, routing.TenantName);
            }

            var paymentRequest = await _paymentRequestRepository.FirstOrDefaultAsync(x => x.Id == paymentRequestId);
            return (paymentRequest?.BookingId, paymentRequest?.ReturnUrl);
        }
        finally
        {
            tenantScope?.Dispose();
        }
    }

    private static string AppendBookingIdQuery(string url, Guid? bookingId)
    {
        if (!bookingId.HasValue || string.IsNullOrWhiteSpace(url))
            return url;

        var sep = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{url}{sep}bookingId={bookingId.Value:D}";
    }

    /// <summary>
    /// Applies VNPAY result to an existing PENDING payment (success or failure). Idempotent if already SUCCESS.
    /// </summary>
    private async Task ApplyVnpGatewayCallbackAsync(
        PaymentRequest paymentRequest,
        Payment payment,
        IReadOnlyDictionary<string, string> query,
        string auditEventType)
    {
        if (payment.PaymentStatus != "PENDING")
            return;

        var payloadJson = JsonSerializer.Serialize(query);
        await InsertAuditAsync(paymentRequest.TenantId, paymentRequest.Id, payloadJson, auditEventType);

        var responseCode = query.GetValueOrDefault("vnp_ResponseCode");
        var transactionStatus = query.GetValueOrDefault("vnp_TransactionStatus");
        var success = responseCode == "00" && transactionStatus == "00";

        var txnNo = query.GetValueOrDefault("vnp_TransactionNo");

        if (success)
        {
            payment.PaymentStatus = "SUCCESS";
            payment.PaidTime = _gmt7Clock.Gmt7Now;
            payment.GatewayTransactionId = txnNo;
            payment.GatewayResponseCode = responseCode;
            payment.GatewayRawResponse = payloadJson;
        }
        else
        {
            payment.PaymentStatus = "FAILED";
            payment.GatewayResponseCode = responseCode;
            payment.GatewayRawResponse = payloadJson;
        }

        await _paymentRepository.UpdateAsync(payment, autoSave: true);

        if (success && paymentRequest.BookingId != Guid.Empty)
        {
            await _customerBookingPaymentNotifier.NotifyBookingPaidAsync(
                paymentRequest.BookingId,
                paymentRequest.Amount,
                paymentRequest.Currency ?? "VND",
                txnNo,
                paymentRequest.Id,
                paymentRequest.TenantId);
        }
    }

    private async Task TryFinalizePaymentFromBrowserReturnAsync(Dictionary<string, string> query)
    {
        if (!VnPayLibrary.HasAnyVnpParameter(query))
            return;

        if (!query.TryGetValue("vnp_TxnRef", out var txnRef) || !Guid.TryParse(txnRef, out var paymentRequestId))
            return;

        if (!query.TryGetValue("vnp_Amount", out var vnpAmountStr))
            return;

        if (!VnPayLibrary.TryParseVnpAmountMajor(vnpAmountStr, out var vnpAmountMajor))
            return;

        IDisposable? tenantScope = null;
        try
        {
            if (MultiTenancyConsts.IsEnabled)
            {
                var routing = await _vnpayTxnRoutingRepository.FindAsync(paymentRequestId);
                if (routing == null)
                    return;

                tenantScope = _currentTenant.Change(routing.TenantId, routing.TenantName);
            }

            var paymentRequest = await _paymentRequestRepository.FirstOrDefaultAsync(x => x.Id == paymentRequestId);
            if (paymentRequest == null)
                return;

            var payment = await _paymentRepository.FirstOrDefaultAsync(x => x.PaymentRequestId == paymentRequest.Id);
            if (payment == null)
                return;

            if (vnpAmountMajor != paymentRequest.Amount)
                return;

            await ApplyVnpGatewayCallbackAsync(paymentRequest, payment, query, auditEventType: "VnpayReturn");
        }
        finally
        {
            tenantScope?.Dispose();
        }
    }

    private async Task InsertAuditAsync(Guid? tenantId, Guid paymentRequestId, string payloadJson, string eventType)
    {
        await _paymentAuditLogRepository.InsertAsync(new PaymentAuditLog
        {
            TenantId = tenantId,
            PaymentRequestId = paymentRequestId,
            EventType = eventType,
            Direction = "Inbound",
            Payload = payloadJson,
            CreatedAt = _gmt7Clock.Gmt7Now
        }, autoSave: true);
    }

    private static VnPayIpnResponseDto Rsp(string code, string message) => new() { RspCode = code, Message = message };

    private static Dictionary<string, string> NormalizeQuery(IReadOnlyDictionary<string, string> queryParameters)
    {
        // Use Ordinal comparer (case-sensitive) to preserve exact parameter names from VNPay
        // VNPay signature validation requires exact parameter name casing
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in queryParameters)
        {
            if (string.IsNullOrEmpty(kv.Key))
                continue;
            dict[kv.Key] = kv.Value ?? string.Empty;
        }

        return dict;
    }

    /// <summary>
    /// VNPAY IPN resolves tenant via <see cref="VnpayTxnRouting"/>; <see cref="TenantName"/> must match SQL schema / ABP tenant name (not a raw GUID).
    /// </summary>
    private string ResolveTenantNameForVnpayRouting()
    {
        if (!string.IsNullOrWhiteSpace(CurrentTenant.Name))
            return CurrentTenant.Name!;

        var http = _httpContextAccessor.HttpContext;
        if (http?.Request.Headers.TryGetValue("X-Tenant", out var header) == true)
        {
            var fromHeader = header.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(fromHeader))
                return fromHeader;
        }

        throw new UserFriendlyException(
            "Cannot resolve tenant for payment callbacks: include tenant name on the token or send the X-Tenant header (same value your DB schema uses, e.g. LTC).");
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        baseUrl = baseUrl.TrimEnd('/');
        path = path.TrimStart('/');
        return $"{baseUrl}/{path}";
    }

    private static string TruncateOrderInfo(string orderInfo)
    {
        if (string.IsNullOrEmpty(orderInfo))
            return "Payment";

        return orderInfo.Length <= 255 ? orderInfo : orderInfo[..255];
    }

    private static bool IsMissingVnpayRoutingTable(Exception ex)
    {
        return ex.ToString().Contains("VnpayTxnRoutings", StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveFrontendOrigin()
    {
        var http = _httpContextAccessor.HttpContext;
        if (http != null)
        {
            if (TryGetOriginFromHeader(http.Request.Headers["Origin"], out var origin))
                return origin;

            if (TryGetOriginFromHeader(http.Request.Headers["Referer"], out var refererOrigin))
                return refererOrigin;
        }

        if (TryGetOriginFromHeader(_options.FrontendSuccessUrl, out var fromSuccess))
            return fromSuccess;

        if (TryGetOriginFromHeader(_options.FrontendFailureUrl, out var fromFailure))
            return fromFailure;

        return string.Empty;
    }

    private static bool TryGetOriginFromHeader(string? raw, out string origin)
    {
        origin = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri))
            return false;

        origin = $"{uri.Scheme}://{uri.Authority}";
        return true;
    }

    public async Task<ManualVnpayCompletionOutputDto> ManualCompleteByBookingAsync(Guid bookingId)
    {
        _logger.LogInformation("Manual VNPay completion requested for booking {BookingId}", bookingId);

        // Find the pending payment request for this booking
        var paymentRequest = await _paymentRequestRepository.FirstOrDefaultAsync(
            x => x.BookingId == bookingId && x.PaymentMethod == "VNPAY");

        if (paymentRequest == null)
        {
            _logger.LogWarning("Manual completion: No VNPAY payment request found for booking {BookingId}", bookingId);
            return new ManualVnpayCompletionOutputDto
            {
                Success = false,
                Message = "No VNPAY payment request found for this booking.",
                BookingId = bookingId
            };
        }

        // Find the associated payment
        var payment = await _paymentRepository.FirstOrDefaultAsync(x => x.PaymentRequestId == paymentRequest.Id);
        if (payment == null)
        {
            _logger.LogWarning("Manual completion: No payment found for payment request {PaymentRequestId}, booking {BookingId}",
                paymentRequest.Id, bookingId);
            return new ManualVnpayCompletionOutputDto
            {
                Success = false,
                Message = "No payment record found.",
                BookingId = bookingId,
                PaymentRequestId = paymentRequest.Id
            };
        }

        _logger.LogInformation(
            "Manual completion: Found payment {PaymentId} with status {PaymentStatus} for booking {BookingId}",
            payment.Id, payment.PaymentStatus, bookingId);

        // If already processed, just return success
        if (payment.PaymentStatus == "SUCCESS")
        {
            _logger.LogInformation("Manual completion: Payment already successful for booking {BookingId}", bookingId);
            return new ManualVnpayCompletionOutputDto
            {
                Success = true,
                Message = "Payment was already successful.",
                BookingId = bookingId,
                PaymentRequestId = paymentRequest.Id,
                PaymentStatus = payment.PaymentStatus,
                NotificationSent = false
            };
        }

        if (payment.PaymentStatus != "PENDING")
        {
            _logger.LogWarning("Manual completion: Payment status is {PaymentStatus}, cannot complete for booking {BookingId}",
                payment.PaymentStatus, bookingId);
            return new ManualVnpayCompletionOutputDto
            {
                Success = false,
                Message = $"Payment status is {payment.PaymentStatus}, cannot complete.",
                BookingId = bookingId,
                PaymentRequestId = paymentRequest.Id,
                PaymentStatus = payment.PaymentStatus,
                NotificationSent = false
            };
        }

        // For pending payments, we cannot verify with VNPay directly without the transaction reference.
        // Instead, we rely on the customer to have completed the payment on VNPay's side.
        // This is a fallback mechanism for when IPN failed.
        // Note: In a production system, you might want to query VNPay's API to verify the transaction.

        _logger.LogWarning(
            "Manual completion: Attempting to complete pending payment for booking {BookingId}. " +
            "This assumes customer completed payment on VNPay. Consider implementing VNPay transaction query API.",
            bookingId);

        // Try to notify customer service to check and update booking status
        // The customer service can verify with its own records
        try
        {
            await _customerBookingPaymentNotifier.NotifyBookingPaidAsync(
                bookingId,
                paymentRequest.Amount,
                paymentRequest.Currency ?? "VND",
                payment.GatewayTransactionId,
                paymentRequest.Id,
                paymentRequest.TenantId);

            _logger.LogInformation("Manual completion: Notification sent to customer service for booking {BookingId}", bookingId);

            return new ManualVnpayCompletionOutputDto
            {
                Success = true,
                Message = "Payment completion notification sent. Please refresh your booking status.",
                BookingId = bookingId,
                PaymentRequestId = paymentRequest.Id,
                PaymentStatus = payment.PaymentStatus,
                NotificationSent = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual completion: Failed to notify customer service for booking {BookingId}", bookingId);
            return new ManualVnpayCompletionOutputDto
            {
                Success = false,
                Message = "Failed to send completion notification. Please contact support.",
                BookingId = bookingId,
                PaymentRequestId = paymentRequest.Id,
                PaymentStatus = payment.PaymentStatus,
                NotificationSent = false
            };
        }
    }
}
