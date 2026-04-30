using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;

namespace LTC.PaymentService.Interfaces;

public interface IPaymentRequestAppService : IApplicationService
{
    Task<PagedResultDto<PaymentRequestOutputDto>> GetPaymentRequestListAsync(GetPaymentRequestListInputDto input);
    Task<PaymentRequestOutputDto> GetPaymentRequestAsync(Guid id);
}
