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
using Volo.Abp.DependencyInjection;

namespace LTC.PaymentService.Services.Integration;

public class CustomerBookingSummaryClient : ICustomerBookingSummaryClient, ITransientDependency
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly IOptions<PaymentCustomerIntegrationOptions> _options;
    private readonly ILogger<CustomerBookingSummaryClient> _logger;

    public CustomerBookingSummaryClient(
        IOptions<PaymentCustomerIntegrationOptions> options,
        ILogger<CustomerBookingSummaryClient> logger)
    {
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
            _logger.LogWarning(
                "Customer booking summaries skipped: BaseUrl={BaseUrl}, HasApiKey={HasApiKey}, BookingIdsCount={Count}",
                opts.CustomerServiceBaseUrl,
                !string.IsNullOrWhiteSpace(opts.CustomerServiceInternalApiKey),
                bookingIds.Count);
            return new Dictionary<Guid, BookingPaymentSummaryRow>();
        }

        var url = $"{opts.CustomerServiceBaseUrl.TrimEnd('/')}/ltc/customer-service/internal/booking/summaries-by-ids";

        _logger.LogInformation(
            "Customer booking summaries request: Url={Url}, BookingIds={BookingIds}, TenantId={TenantId}",
            url,
            string.Join(",", bookingIds),
            tenantId);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", opts.CustomerServiceInternalApiKey);
            if (tenantId.HasValue)
            {
                request.Headers.TryAddWithoutValidation("__tenant", tenantId.Value.ToString());
            }

            request.Content = JsonContent.Create(new { bookingIds = bookingIds.Distinct().ToList() });

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Customer booking summaries failed: Status={Status}, Body={Body}",
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
                _logger.LogWarning(
                    "Customer booking summaries returned empty data: EnvelopeNull={EnvelopeNull}, ListCount={ListCount}",
                    envelope == null,
                    list?.Count ?? 0);
                return new Dictionary<Guid, BookingPaymentSummaryRow>();
            }

            _logger.LogInformation(
                "Customer booking summaries succeeded: ReturnedCount={ReturnedCount}, RequestedCount={RequestedCount}",
                list.Count,
                bookingIds.Count);

            return list
                .GroupBy(x => x.BookingId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var x = g.First();
                        return new BookingPaymentSummaryRow
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
                            MovieName = x.MovieName,
                            CinemaName = x.CinemaName,
                        };
                    });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer booking summaries request threw exception");
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
        public string? MovieName { get; set; }
        public string? CinemaName { get; set; }
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
