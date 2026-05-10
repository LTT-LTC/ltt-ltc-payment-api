using System;
using Volo.Abp.Application.Dtos;

namespace LTC.PaymentService.Dtos.Output;

public class PaymentOutputDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }
    public Guid PaymentRequestId { get; set; }
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? PaidTime { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? GatewayResponseCode { get; set; }
    public string? GatewayRawResponse { get; set; }

    /// <summary>From <c>PaymentRequests</c> — populated when joined.</summary>
    public string? Currency { get; set; }
    public string? PaymentGateway { get; set; }
    public string? GatewayOrderId { get; set; }
    public DateTime? PaymentRequestCreatedAt { get; set; }
    public DateTime? PaymentRequestExpiredAt { get; set; }

    /// <summary>From customer-service <c>Bookings</c> — populated via internal batch API when configured.</summary>
    public Guid? BookingShowtimeId { get; set; }
    public string? BookingStatus { get; set; }
    public string? BookingSeatCodes { get; set; }
    public decimal? BookingTotalPrice { get; set; }
    public decimal? BookingPaidAmount { get; set; }
    public decimal? BookingDiscountAmount { get; set; }
    public DateTime? BookingCreatedAt { get; set; }
    public DateTime? BookingExpiredAt { get; set; }
    public string? BookingSnapshotJson { get; set; }
}
