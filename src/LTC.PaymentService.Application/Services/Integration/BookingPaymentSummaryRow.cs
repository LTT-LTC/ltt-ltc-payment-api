using System;

namespace LTC.PaymentService.Services.Integration;

/// <summary>Normalized booking summary merged into <see cref="LTC.PaymentService.Dtos.Output.PaymentOutputDto"/>.</summary>
public sealed class BookingPaymentSummaryRow
{
    public Guid ShowtimeId { get; set; }
    public string? BookingStatus { get; set; }
    public string? SeatCodes { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public string? SnapshotJson { get; set; }
}
