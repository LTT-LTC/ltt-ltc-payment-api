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
}
