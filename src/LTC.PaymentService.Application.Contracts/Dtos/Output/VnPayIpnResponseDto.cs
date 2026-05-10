namespace LTC.PaymentService.Dtos.Output;

/// <summary>
/// VNPAY IPN acknowledgement body (always HTTP 200 with JSON).
/// </summary>
public class VnPayIpnResponseDto
{
    public string RspCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
