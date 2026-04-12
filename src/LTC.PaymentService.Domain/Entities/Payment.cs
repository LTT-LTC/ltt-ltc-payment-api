using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace LTC.PaymentService.Entities;

public class Payment : Entity<Guid>, IMultiTenant
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
}
