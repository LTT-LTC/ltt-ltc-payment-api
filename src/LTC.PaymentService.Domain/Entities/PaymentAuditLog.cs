using System;
using Volo.Abp.Domain.Entities;

namespace LTC.PaymentService.Entities;

public class PaymentAuditLog : Entity<Guid>
{
    public Guid PaymentRequestId { get; set; }
    public string? EventType { get; set; }
    public string? Direction { get; set; }
    public string? Payload { get; set; }
    public DateTime? CreatedAt { get; set; }
}
