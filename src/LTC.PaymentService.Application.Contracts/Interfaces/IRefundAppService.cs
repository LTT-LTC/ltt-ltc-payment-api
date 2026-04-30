using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;

namespace LTC.PaymentService.Interfaces;

public interface IRefundAppService : IApplicationService
{
    Task<PagedResultDto<RefundOutputDto>> GetRefundListAsync(GetRefundListInputDto input);
    Task<RefundOutputDto> GetRefundAsync(Guid id);
    Task<RefundOutputDto> InitiateRefundAsync(CreateRefundInputDto input);
}
