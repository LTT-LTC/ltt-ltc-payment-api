namespace LTC.PaymentService.Dtos.Output;

/// <summary>
/// Result of manually checking and completing a VNPay payment.
/// </summary>
public class ManualVnpayCompletionOutputDto
{
    /// <summary>
    /// Whether the completion was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message about the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The booking ID that was checked.
    /// </summary>
    public Guid BookingId { get; set; }

    /// <summary>
    /// The payment request ID associated with the booking.
    /// </summary>
    public Guid? PaymentRequestId { get; set; }

    /// <summary>
    /// Current payment status (PENDING, SUCCESS, FAILED, etc.)
    /// </summary>
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// Whether the booking notification was sent to customer service.
    /// </summary>
    public bool NotificationSent { get; set; }
}
