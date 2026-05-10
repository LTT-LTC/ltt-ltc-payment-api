using System;

namespace LTC.PaymentService.Dtos.Input;

public class CreateVnPayPaymentUrlInputDto
{
    public Guid BookingId { get; set; }

    /// <summary>
    /// Amount in VND (major units).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Order description shown on VNPAY (max 255 characters).
    /// </summary>
    public string OrderInfo { get; set; } = string.Empty;

    /// <summary>
    /// Optional locale: vn or en (default vn).
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Optional VNPAY channel, e.g. VNPAYQR, VNBANK, INTCARD (same as official demo).
    /// </summary>
    public string? BankCode { get; set; }
}
