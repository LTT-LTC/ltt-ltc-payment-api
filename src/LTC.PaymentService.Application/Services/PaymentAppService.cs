using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Entities;
using LTC.PaymentService.Interfaces;
using LTC.PaymentService.Services.Integration;
using Microsoft.EntityFrameworkCore;

namespace LTC.PaymentService.Services;

public class PaymentAppService : ApplicationService, IPaymentAppService
{
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<PaymentRequest, Guid> _paymentRequestRepository;
    private readonly IRepository<PaymentAuditLog, Guid> _auditLogRepository;
    private readonly ICustomerBookingSummaryClient _bookingSummaryClient;

    public PaymentAppService(
        IRepository<Payment, Guid> paymentRepository,
        IRepository<PaymentRequest, Guid> paymentRequestRepository,
        IRepository<PaymentAuditLog, Guid> auditLogRepository,
        ICustomerBookingSummaryClient bookingSummaryClient)
    {
        _paymentRepository = paymentRepository;
        _paymentRequestRepository = paymentRequestRepository;
        _auditLogRepository = auditLogRepository;
        _bookingSummaryClient = bookingSummaryClient;
    }

    public async Task<PagedResultDto<PaymentOutputDto>> GetPaymentListAsync(GetPaymentListInputDto input)
    {
        var queryable = await _paymentRepository.GetQueryableAsync();

        if (input.CustomerId.HasValue)
        {
            var requestQueryable = await _paymentRequestRepository.GetQueryableAsync();
            var ownedIds = requestQueryable
                .Where(pr => pr.CustomerId == input.CustomerId.Value)
                .Select(pr => pr.Id);
            queryable = queryable.Where(p => ownedIds.Contains(p.PaymentRequestId));
        }

        if (!string.IsNullOrWhiteSpace(input.Status))
        {
            queryable = queryable.Where(x => x.PaymentStatus == input.Status);
        }
        if (input.FromDate.HasValue)
        {
            queryable = queryable.Where(x => x.PaidTime >= input.FromDate.Value);
        }
        if (input.ToDate.HasValue)
        {
            queryable = queryable.Where(x => x.PaidTime <= input.ToDate.Value);
        }

        var totalCount = await queryable.CountAsync();
        var items = await queryable.OrderBy(input.Sorting).PageBy(input.SkipCount, input.MaxResultCount).ToListAsync();

        var dtos = MapPaymentsToDtos(items);
        await ApplyPaymentRequestEnrichmentAsync(items, dtos);
        await ApplyBookingEnrichmentAsync(dtos);

        return new PagedResultDto<PaymentOutputDto>(
            totalCount,
            dtos
        );
    }

    public async Task<PaymentOutputDto> GetPaymentAsync(Guid id)
    {
        var entity = await _paymentRepository.GetAsync(id);
        var dto = MapPaymentToDto(entity);
        await ApplyPaymentRequestEnrichmentAsync(new List<Payment> { entity }, new List<PaymentOutputDto> { dto });
        await ApplyBookingEnrichmentAsync(new List<PaymentOutputDto> { dto });
        return dto;
    }

    public async Task<PagedResultDto<PaymentAuditLogOutputDto>> GetAuditLogsAsync(Guid paymentId, PaginationInputDto input)
    {
        var payment = await _paymentRepository.GetAsync(paymentId);

        var queryable = await _auditLogRepository.GetQueryableAsync();
        queryable = queryable.Where(x => x.PaymentRequestId == payment.PaymentRequestId);

        var totalCount = await queryable.CountAsync();
        var items = await queryable.OrderBy(input.Sorting != "Id ASC" && input.Sorting != "Id DESC" ? input.Sorting : "CreatedAt DESC").PageBy(input.SkipCount, input.MaxResultCount).ToListAsync();

        return new PagedResultDto<PaymentAuditLogOutputDto>(
            totalCount,
            items.Select(MapAuditLogToDto).ToList()
        );
    }

    public async Task<PagedResultDto<PaymentOutputDto>> GetMyPaymentsAsync(GetPaymentListInputDto input)
    {
        if (!CurrentUser.IsAuthenticated || !CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Authentication required.");
        }

        var customerId = CurrentUser.GetId();
        var queryable = await _paymentRepository.GetQueryableAsync();
        var requestQueryable = await _paymentRequestRepository.GetQueryableAsync();

        var ownedRequests = requestQueryable.Where(pr => pr.CustomerId == customerId);
        if (CurrentTenant.Id.HasValue)
        {
            ownedRequests = ownedRequests.Where(pr => pr.TenantId == CurrentTenant.Id);
        }

        var ownedRequestIds = ownedRequests.Select(pr => pr.Id);
        queryable = queryable.Where(p => ownedRequestIds.Contains(p.PaymentRequestId));

        if (CurrentTenant.Id.HasValue)
        {
            queryable = queryable.Where(p => p.TenantId == CurrentTenant.Id);
        }

        if (!string.IsNullOrWhiteSpace(input.Status))
        {
            queryable = queryable.Where(x => x.PaymentStatus == input.Status);
        }

        if (input.FromDate.HasValue)
        {
            queryable = queryable.Where(x => x.PaidTime >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            queryable = queryable.Where(x => x.PaidTime <= input.ToDate.Value);
        }

        var totalCount = await queryable.CountAsync();
        var items = await queryable.OrderBy(input.Sorting).PageBy(input.SkipCount, input.MaxResultCount).ToListAsync();

        var dtos = MapPaymentsToDtos(items);
        await ApplyPaymentRequestEnrichmentAsync(items, dtos);
        await ApplyBookingEnrichmentAsync(dtos);

        return new PagedResultDto<PaymentOutputDto>(
            totalCount,
            dtos
        );
    }

    public async Task<PaymentOutputDto> GetMyPaymentAsync(Guid id)
    {
        if (!CurrentUser.IsAuthenticated || !CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Authentication required.");
        }

        var customerId = CurrentUser.GetId();
        var entity = await _paymentRepository.GetAsync(id);
        var request = await _paymentRequestRepository.GetAsync(entity.PaymentRequestId);

        if (request.CustomerId != customerId)
        {
            throw new AbpAuthorizationException("You do not have access to this payment.");
        }

        if (CurrentTenant.Id.HasValue &&
            (entity.TenantId != CurrentTenant.Id || request.TenantId != CurrentTenant.Id))
        {
            throw new AbpAuthorizationException("You do not have access to this payment.");
        }

        var dto = MapPaymentToDto(entity);
        await ApplyPaymentRequestEnrichmentAsync(new List<Payment> { entity }, new List<PaymentOutputDto> { dto });
        await ApplyBookingEnrichmentAsync(new List<PaymentOutputDto> { dto });
        return dto;
    }

    public async Task<DashboardSummaryOutputDto> GetDashboardSummaryAsync(DashboardSummaryInputDto input)
    {
        var queryable = await _paymentRepository.GetQueryableAsync();
        queryable = queryable.Where(x => x.PaymentStatus == "SUCCESS");

        if (input.FromDate != default)
            queryable = queryable.Where(x => x.PaidTime >= input.FromDate);
        if (input.ToDate != default)
            queryable = queryable.Where(x => x.PaidTime <= input.ToDate);

        var payments = await queryable.OrderBy(x => x.PaidTime).ToListAsync();
        var dtos = MapPaymentsToDtos(payments);
        await ApplyPaymentRequestEnrichmentAsync(payments, dtos);
        await ApplyBookingEnrichmentAsync(dtos);

        var result = new DashboardSummaryOutputDto();

        var dailyMap = new Dictionary<string, DailyRevenueDto>();
        var hourlyMap = new Dictionary<string, HourlyRevenueDto>();
        var movieMap = new Dictionary<string, MovieRevenueDto>();

        foreach (var dto in dtos)
        {
            var paidTime = dto.PaidTime ?? dto.BookingCreatedAt;
            if (paidTime == null) continue;

            decimal ticketRev = dto.Amount;
            decimal fnbRev = 0;
            decimal totalRev = dto.Amount;
            int seatCount = 1;

            if (!string.IsNullOrEmpty(dto.BookingSnapshotJson))
            {
                try
                {
                    var snap = System.Text.Json.JsonSerializer.Deserialize<BookingSnapshotData>(
                        dto.BookingSnapshotJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (snap?.TicketTotal.HasValue == true)
                    {
                        ticketRev = snap.TicketTotal.Value;
                        fnbRev = snap.ExtrasTotal ?? 0;
                        totalRev = snap.GrandTotal ?? (ticketRev + fnbRev);
                    }
                    if (snap?.Seats != null && snap.Seats.Count > 0)
                        seatCount = snap.Seats.Count;
                }
                catch { /* ignore parse errors */ }
            }
            else if (!string.IsNullOrEmpty(dto.BookingSeatCodes))
            {
                seatCount = dto.BookingSeatCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            }

            result.Totals.TotalRevenue += totalRev;
            result.Totals.TicketRevenue += ticketRev;
            result.Totals.FnbRevenue += fnbRev;
            result.Totals.TicketsSold += seatCount;
            result.Totals.TransactionCount++;

            var dateKey = paidTime.Value.ToString("yyyy-MM-dd");
            if (!dailyMap.TryGetValue(dateKey, out var dailyRow))
            {
                dailyRow = new DailyRevenueDto { Date = dateKey };
                dailyMap[dateKey] = dailyRow;
            }
            dailyRow.TicketRevenue += ticketRev;
            dailyRow.FnbRevenue += fnbRev;
            dailyRow.TotalRevenue += totalRev;
            dailyRow.TicketsSold += seatCount;

            var hourKey = paidTime.Value.ToString("HH");
            if (!hourlyMap.TryGetValue(hourKey, out var hourlyRow))
            {
                hourlyRow = new HourlyRevenueDto { Hour = hourKey };
                hourlyMap[hourKey] = hourlyRow;
            }
            hourlyRow.Revenue += totalRev;
            hourlyRow.TransactionCount++;

            var showtimeId = dto.BookingShowtimeId?.ToString() ?? "unknown";
            if (!movieMap.TryGetValue(showtimeId, out var movieRow))
            {
                movieRow = new MovieRevenueDto { ShowtimeId = dto.BookingShowtimeId };
                movieMap[showtimeId] = movieRow;
            }
            movieRow.Revenue += totalRev;
            movieRow.TicketsSold += seatCount;
        }

        result.DailyBreakdown = dailyMap.Values.OrderBy(d => d.Date).ToList();

        for (int h = 0; h < 24; h++)
        {
            var key = h.ToString("D2");
            if (!hourlyMap.ContainsKey(key))
                hourlyMap[key] = new HourlyRevenueDto { Hour = key };
        }
        result.HourlyTrend = hourlyMap.Values.OrderBy(h => h.Hour).ToList();

        result.TopMovies = movieMap.Values.OrderByDescending(m => m.Revenue).Take(10).ToList();

        return result;
    }

    /// <summary>
    /// Explicit mapping avoids reliance on Mapperly list-registration at runtime (ObjectMapper often misses List{T}-to-List{T}).
    /// </summary>
    private static List<PaymentOutputDto> MapPaymentsToDtos(IEnumerable<Payment> payments) =>
        payments.Select(MapPaymentToDto).ToList();

    private static PaymentOutputDto MapPaymentToDto(Payment p)
    {
        return new PaymentOutputDto
        {
            Id = p.Id,
            TenantId = p.TenantId,
            PaymentRequestId = p.PaymentRequestId,
            BookingId = p.BookingId,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            PaymentStatus = p.PaymentStatus,
            PaidTime = p.PaidTime,
            GatewayTransactionId = p.GatewayTransactionId,
            GatewayResponseCode = p.GatewayResponseCode,
            GatewayRawResponse = p.GatewayRawResponse,
        };
    }

    private static PaymentAuditLogOutputDto MapAuditLogToDto(PaymentAuditLog x)
    {
        return new PaymentAuditLogOutputDto
        {
            Id = x.Id,
            PaymentRequestId = x.PaymentRequestId,
            EventType = x.EventType,
            Direction = x.Direction,
            Payload = x.Payload,
            CreatedAt = x.CreatedAt,
        };
    }

    private async Task ApplyPaymentRequestEnrichmentAsync(IReadOnlyList<Payment> payments, List<PaymentOutputDto> dtos)
    {
        if (payments.Count == 0)
        {
            return;
        }

        var reqIds = payments.Select(p => p.PaymentRequestId).Distinct().ToList();
        var reqs = await _paymentRequestRepository.GetListAsync(x => reqIds.Contains(x.Id));
        var dict = reqs.ToDictionary(x => x.Id);

        for (var i = 0; i < payments.Count; i++)
        {
            if (dict.TryGetValue(payments[i].PaymentRequestId, out var pr))
            {
                ApplyPaymentRequestFields(dtos[i], pr);
            }
        }
    }

    private static void ApplyPaymentRequestFields(PaymentOutputDto dto, PaymentRequest pr)
    {
        dto.Currency = pr.Currency;
        dto.PaymentGateway = pr.PaymentGateway;
        dto.GatewayOrderId = pr.GatewayOrderId;
        dto.PaymentRequestCreatedAt = pr.CreatedAt;
        dto.PaymentRequestExpiredAt = pr.ExpiredAt;
    }

    private async Task ApplyBookingEnrichmentAsync(List<PaymentOutputDto> dtos)
    {
        if (dtos.Count == 0)
        {
            return;
        }

        var bookingIds = dtos.Select(d => d.BookingId).Distinct().ToList();
        var map = await _bookingSummaryClient.GetSummariesAsync(bookingIds, CurrentTenant.Id);

        foreach (var dto in dtos)
        {
            if (!map.TryGetValue(dto.BookingId, out var b))
            {
                continue;
            }

            dto.BookingShowtimeId = b.ShowtimeId;
            dto.BookingStatus = b.BookingStatus;
            dto.BookingSeatCodes = b.SeatCodes;
            dto.BookingTotalPrice = b.TotalPrice;
            dto.BookingPaidAmount = b.PaidAmount;
            dto.BookingDiscountAmount = b.DiscountAmount;
            dto.BookingCreatedAt = b.CreatedAt;
            dto.BookingExpiredAt = b.ExpiredAt;
            dto.BookingSnapshotJson = b.SnapshotJson;
            dto.MovieName = b.MovieName;
            dto.CinemaName = b.CinemaName;
        }
    }

    private class BookingSnapshotData
    {
        public decimal? GrandTotal { get; set; }
        public decimal? TicketTotal { get; set; }
        public decimal? ExtrasTotal { get; set; }
        public decimal? Discount { get; set; }
        public List<string>? Seats { get; set; }
    }
}
