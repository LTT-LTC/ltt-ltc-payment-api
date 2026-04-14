using System;

namespace LTC.PaymentService.Dtos.Input;

public class GetRefundListInputDto : PaginationInputDto
{
    public string? Status { get; set; }
}
