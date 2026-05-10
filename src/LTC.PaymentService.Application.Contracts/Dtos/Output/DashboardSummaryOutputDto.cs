using System;
using System.Collections.Generic;

namespace LTC.PaymentService.Dtos.Output;

public class DashboardSummaryOutputDto
{
    public DashboardTotalsDto Totals { get; set; } = new();
    public List<DailyRevenueDto> DailyBreakdown { get; set; } = new();
    public List<HourlyRevenueDto> HourlyTrend { get; set; } = new();
    public List<MovieRevenueDto> TopMovies { get; set; } = new();
}

public class DashboardTotalsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TicketRevenue { get; set; }
    public decimal FnbRevenue { get; set; }
    public int TicketsSold { get; set; }
    public int TransactionCount { get; set; }
}

public class DailyRevenueDto
{
    public string Date { get; set; } = string.Empty;
    public decimal TicketRevenue { get; set; }
    public decimal FnbRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TicketsSold { get; set; }
}

public class HourlyRevenueDto
{
    public string Hour { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}

public class MovieRevenueDto
{
    public string MovieTitle { get; set; } = string.Empty;
    public Guid? ShowtimeId { get; set; }
    public decimal Revenue { get; set; }
    public int TicketsSold { get; set; }
}
