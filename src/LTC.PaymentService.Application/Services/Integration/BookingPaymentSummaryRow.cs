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

    /// <summary>Movie name for the showtime associated with this booking.</summary>
    public string? MovieName { get; set; }

    /// <summary>Cinema name for the showtime associated with this booking.</summary>
    public string? CinemaName { get; set; }
}
