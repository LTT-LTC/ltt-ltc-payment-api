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
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

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

    public VnPayAppService(
        IOptions<VnPayOptions> options,
        IGmt7Clock gmt7Clock,
        IRepository<PaymentRequest, Guid> paymentRequestRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<PaymentAuditLog, Guid> paymentAuditLogRepository,
        IRepository<VnpayTxnRouting, Guid> vnpayTxnRoutingRepository,
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor,
        ICustomerBookingPaymentNotifier customerBookingPaymentNotifier)
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
            ReturnUrl = returnFullUrl,
            NotifyUrl = ipnFullUrl,
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
            await _vnpayTxnRoutingRepository.InsertAsync(new VnpayTxnRouting(paymentRequest.Id)
            {
                TenantId = CurrentTenant.Id!.Value,
                TenantName = ResolveTenantNameForVnpayRouting()
            }, autoSave: true);
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
        var query = NormalizeQuery(queryParameters);

        if (!VnPayLibrary.HasAnyVnpParameter(query))
            return Rsp("99", "Input data required");

        if (!VnPayLibrary.ValidateSignature(query, _options.HashSecret, out _))
            return Rsp("97", "Invalid Signature");

        if (!query.TryGetValue("vnp_TxnRef", out var txnRef) || !Guid.TryParse(txnRef, out var paymentRequestId))
            return Rsp("01", "Order not found");

        if (!query.TryGetValue("vnp_Amount", out var vnpAmountStr))
            return Rsp("04", "Invalid amount");

        if (!VnPayLibrary.TryParseVnpAmountMajor(vnpAmountStr, out var vnpAmountMajor))
            return Rsp("04", "Invalid amount");

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
                return Rsp("01", "Order not found");

            var payment = await _paymentRepository.FirstOrDefaultAsync(x => x.PaymentRequestId == paymentRequest.Id);
            if (payment == null)
                return Rsp("01", "Order not found");

            if (vnpAmountMajor != paymentRequest.Amount)
                return Rsp("04", "Invalid amount");

            if (payment.PaymentStatus == "SUCCESS")
                return Rsp("02", "Order already confirmed");

            if (payment.PaymentStatus == "FAILED")
                return Rsp("00", "Confirm success");

            await ApplyVnpGatewayCallbackAsync(paymentRequest, payment, query, auditEventType: "VnpayIpn");

            return Rsp("00", "Confirm success");
        }
        finally
        {
            tenantScope?.Dispose();
        }
    }

    public async Task<string> ProcessReturnAsync(IReadOnlyDictionary<string, string> queryParameters)
    {
        var query = NormalizeQuery(queryParameters);

        if (!VnPayLibrary.ValidateSignature(query, _options.HashSecret, out _))
            return _options.FrontendFailureUrl;

        // Same callback params as IPN: persist SUCCESS/FAILED when IPN never reaches this host (localhost, firewall).
        await TryFinalizePaymentFromBrowserReturnAsync(query);

        var responseCode = query.GetValueOrDefault("vnp_ResponseCode");
        var transactionStatus = query.GetValueOrDefault("vnp_TransactionStatus");
        var success = responseCode == "00" && transactionStatus == "00";

        var baseUrl = success ? _options.FrontendSuccessUrl : _options.FrontendFailureUrl;

        if (!Guid.TryParse(query.GetValueOrDefault("vnp_TxnRef"), out var paymentRequestId))
            return baseUrl;

        var bookingId = await TryResolveBookingIdAsync(paymentRequestId);
        return AppendBookingIdQuery(baseUrl, bookingId);
    }

    private async Task<Guid?> TryResolveBookingIdAsync(Guid paymentRequestId)
    {
        IDisposable? tenantScope = null;
        try
        {
            if (MultiTenancyConsts.IsEnabled)
            {
                var routing = await _vnpayTxnRoutingRepository.FindAsync(paymentRequestId);
                if (routing == null)
                    return null;

                tenantScope = _currentTenant.Change(routing.TenantId, routing.TenantName);
            }

            var paymentRequest = await _paymentRequestRepository.FirstOrDefaultAsync(x => x.Id == paymentRequestId);
            return paymentRequest?.BookingId;
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
                paymentRequest.Id);
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
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
}
