using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;

namespace LTC.PaymentService.Interfaces;

public interface IPaymentAppService : IApplicationService
{
    Task<PagedResultDto<PaymentOutputDto>> GetListAsync(GetPaymentListInputDto input);
    Task<PaymentOutputDto> GetAsync(Guid id);
    Task<PagedResultDto<PaymentAuditLogOutputDto>> GetAuditLogsAsync(Guid paymentId, PaginationInputDto input);
}
