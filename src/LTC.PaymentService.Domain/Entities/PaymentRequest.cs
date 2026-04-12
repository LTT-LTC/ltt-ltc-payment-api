using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace LTC.PaymentService.Entities;

public class PaymentRequest : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid BookingId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? PaymentGateway { get; set; }
    public string? GatewayOrderId { get; set; }
    public string? ReturnUrl { get; set; }
    public string? NotifyUrl { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}
