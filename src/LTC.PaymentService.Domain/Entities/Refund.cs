using System;
using Volo.Abp.Domain.Entities;

namespace LTC.PaymentService.Entities;

public class Refund : Entity<Guid>
{
    public Guid BookingId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
