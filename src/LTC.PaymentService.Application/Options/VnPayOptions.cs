namespace LTC.PaymentService.Options;

public class VnPayOptions
{
    public const string SectionName = "VnPay";

    /// <summary>
    /// Merchant terminal code from VNPAY.
    /// </summary>
    public string TmnCode { get; set; } = string.Empty;

    /// <summary>
    /// Secret key for HMAC-SHA512 signing.
    /// </summary>
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>
    /// VNPAY payment page URL (sandbox or production).
    /// </summary>
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    /// <summary>
    /// Public base URL of this payment API as seen by browsers and VNPAY (no trailing slash).
    /// Example: https://api.example.com/ltc/payment-service
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Path appended to <see cref="PublicBaseUrl"/> for the browser return handler (leading slash optional).
    /// </summary>
    public string ReturnPath { get; set; } = "/api/payment/vnpay-return";

    /// <summary>
    /// Path appended to <see cref="PublicBaseUrl"/> for server-side IPN (leading slash optional).
    /// </summary>
    public string IpnPath { get; set; } = "/api/payment/vnpay-ipn";

    /// <summary>
    /// Frontend URL when payment succeeded (user redirect only; money state is updated via IPN).
    /// </summary>
    public string FrontendSuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// Frontend URL when payment failed or return verification failed.
    /// </summary>
    public string FrontendFailureUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional payment request expiry from creation time (GMT+7).
    /// </summary>
    public int OrderExpireMinutes { get; set; } = 15;
}
