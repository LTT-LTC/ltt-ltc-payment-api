using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;

namespace LTC.PaymentService.Interfaces;

public interface IRefundAppService : IApplicationService
{
    Task<PagedResultDto<RefundOutputDto>> GetListAsync(GetRefundListInputDto input);
    Task<RefundOutputDto> GetAsync(Guid id);
    Task<RefundOutputDto> InitiateRefundAsync(CreateRefundInputDto input);
}
