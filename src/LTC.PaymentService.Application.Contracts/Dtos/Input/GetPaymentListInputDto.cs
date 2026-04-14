using System;

namespace LTC.PaymentService.Dtos.Input;

public class GetPaymentListInputDto : PaginationInputDto
{
    public Guid? CustomerId { get; set; }
    public Guid? CinemaId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
