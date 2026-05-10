namespace LTC.PaymentService.Options;

/// <summary>HTTP callbacks from payment-service to customer-service after gateway events.</summary>
public class PaymentCustomerIntegrationOptions
{
    public const string SectionName = "Integration";

    /// <summary>Customer API origin only, e.g. https://host:port (no trailing path).</summary>
    public string CustomerServiceBaseUrl { get; set; } = string.Empty;

    /// <summary>Must match customer host InternalApi:PaymentServiceApiKey.</summary>
    public string CustomerServiceInternalApiKey { get; set; } = string.Empty;
}
