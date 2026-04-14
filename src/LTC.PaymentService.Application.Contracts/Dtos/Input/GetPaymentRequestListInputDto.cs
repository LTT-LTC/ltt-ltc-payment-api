using System;

namespace LTC.PaymentService.Dtos.Input;

public class GetPaymentRequestListInputDto : PaginationInputDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
