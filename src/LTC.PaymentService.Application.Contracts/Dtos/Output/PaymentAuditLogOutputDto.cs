using System;
using Volo.Abp.Application.Dtos;

namespace LTC.PaymentService.Dtos.Output;

public class PaymentAuditLogOutputDto : EntityDto<Guid>
{
    public Guid PaymentRequestId { get; set; }
    public string? EventType { get; set; }
    public string? Direction { get; set; }
    public string? Payload { get; set; }
    public DateTime? CreatedAt { get; set; }
}
