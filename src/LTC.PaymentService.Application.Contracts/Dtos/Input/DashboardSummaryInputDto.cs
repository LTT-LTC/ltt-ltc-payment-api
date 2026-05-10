using System;

namespace LTC.PaymentService.Dtos.Input;

public class DashboardSummaryInputDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? CinemaId { get; set; }
}
