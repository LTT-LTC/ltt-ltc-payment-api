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
}
