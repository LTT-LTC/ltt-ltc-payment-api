using System;

namespace LTC.PaymentService.Dtos.Output;

public class PaymentStatusByBookingOutputDto
{
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string? GatewayTransactionId { get; set; }
    public Guid? PaymentRequestId { get; set; }
    public Guid? TenantId { get; set; }
}
