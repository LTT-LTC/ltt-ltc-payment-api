using System;

namespace LTC.PaymentService.Dtos.Input;

public class CreateRefundInputDto
{
    public Guid BookingId { get; set; }
    public string? Reason { get; set; }
    public decimal Amount { get; set; }
}
