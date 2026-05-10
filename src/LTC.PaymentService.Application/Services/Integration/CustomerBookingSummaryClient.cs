using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LTC.PaymentService.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace LTC.PaymentService.Services.Integration;

public class CustomerBookingSummaryClient : ICustomerBookingSummaryClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<PaymentCustomerIntegrationOptions> _options;
    private readonly ILogger<CustomerBookingSummaryClient> _logger;

    public CustomerBookingSummaryClient(
        IHttpClientFactory httpClientFactory,
        IOptions<PaymentCustomerIntegrationOptions> options,
        ILogger<CustomerBookingSummaryClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<Guid, BookingPaymentSummaryRow>> GetSummariesAsync(
        IReadOnlyList<Guid> bookingIds,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (string.IsNullOrWhiteSpace(opts.CustomerServiceBaseUrl) ||
            string.IsNullOrWhiteSpace(opts.CustomerServiceInternalApiKey) ||
            bookingIds.Count == 0)
        {
            return new Dictionary<Guid, BookingPaymentSummaryRow>();
        }

        var client = _httpClientFactory.CreateClient(nameof(CustomerBookingSummaryClient));
        var url = $"{opts.CustomerServiceBaseUrl.TrimEnd('/')}/ltc/customer-service/internal/booking/summaries-by-ids";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", opts.CustomerServiceInternalApiKey);
            if (tenantId.HasValue)
            {
                request.Headers.TryAddWithoutValidation("__tenant", tenantId.Value.ToString());
            }

            request.Content = JsonContent.Create(new { bookingIds = bookingIds.Distinct().ToList() });

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Customer booking summaries failed: {Status} {Body}",
                    (int)response.StatusCode,
                    body);
                return new Dictionary<Guid, BookingPaymentSummaryRow>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var envelope = await JsonSerializer.DeserializeAsync<CustomerApiEnvelope<List<RemoteBookingSummaryDto>>>(
                stream,
                JsonSerializerOptionsCache.Options,
                cancellationToken);

            var list = envelope?.Data;
            if (list == null || list.Count == 0)
            {
                return new Dictionary<Guid, BookingPaymentSummaryRow>();
            }

            return list.ToDictionary(
                x => x.BookingId,
                x => new BookingPaymentSummaryRow
                {
                    ShowtimeId = x.ShowtimeId,
                    BookingStatus = x.BookingStatus,
                    SeatCodes = x.SeatCodes,
                    TotalPrice = x.TotalPrice,
                    PaidAmount = x.PaidAmount,
                    DiscountAmount = x.DiscountAmount,
                    CreatedAt = x.CreatedAt,
                    ExpiredAt = x.ExpiredAt,
                    SnapshotJson = x.SnapshotJson,
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Customer booking summaries request threw");
            return new Dictionary<Guid, BookingPaymentSummaryRow>();
        }
    }

    private sealed class CustomerApiEnvelope<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private sealed class RemoteBookingSummaryDto
    {
        public Guid BookingId { get; set; }
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

    private static class JsonSerializerOptionsCache
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }
}
