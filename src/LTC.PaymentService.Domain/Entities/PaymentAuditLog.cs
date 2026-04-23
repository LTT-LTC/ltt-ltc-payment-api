using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace LTC.PaymentService.Entities;

public class PaymentAuditLog : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid PaymentRequestId { get; set; }
    public string? EventType { get; set; }
    public string? Direction { get; set; }
    public string? Payload { get; set; }
    public DateTime? CreatedAt { get; set; }
}
