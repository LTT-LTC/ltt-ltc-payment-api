using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;

namespace LTC.PaymentService.Interfaces;

public interface IPaymentAppService : IApplicationService
{
    Task<PagedResultDto<PaymentOutputDto>> GetPaymentListAsync(GetPaymentListInputDto input);
    Task<PaymentOutputDto> GetPaymentAsync(Guid id);
    Task<PagedResultDto<PaymentAuditLogOutputDto>> GetAuditLogsAsync(Guid paymentId, PaginationInputDto input);

    /// <summary>Paged payments for the authenticated customer only (JWT user id + tenant).</summary>
    Task<PagedResultDto<PaymentOutputDto>> GetMyPaymentsAsync(GetPaymentListInputDto input);

    /// <summary>Single payment if it belongs to the authenticated customer.</summary>
    Task<PaymentOutputDto> GetMyPaymentAsync(Guid id);

    /// <summary>Aggregated dashboard summary for admin/manager (revenue, tickets, hourly trend, top movies).</summary>
    Task<DashboardSummaryOutputDto> GetDashboardSummaryAsync(DashboardSummaryInputDto input);
}
