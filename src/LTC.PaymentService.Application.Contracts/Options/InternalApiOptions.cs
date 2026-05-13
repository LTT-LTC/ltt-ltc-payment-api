namespace LTC.PaymentService.Options;

/// <summary>API keys for inbound service-to-service calls into payment-service.</summary>
public class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    /// <summary>API key expected from customer-service when calling internal payment endpoints.</summary>
    public string CustomerServiceApiKey { get; set; } = string.Empty;
}
