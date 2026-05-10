using System.Collections.Generic;
using System.Threading.Tasks;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using Volo.Abp.Application.Services;

namespace LTC.PaymentService.Interfaces;

public interface IVnPayAppService : IApplicationService
{
    Task<CreateVnPayPaymentUrlOutputDto> CreatePaymentUrlAsync(
        CreateVnPayPaymentUrlInputDto input,
        string clientIpAddress);

    Task<VnPayIpnResponseDto> ProcessIpnAsync(IReadOnlyDictionary<string, string> queryParameters);

    /// <summary>
    /// Returns an absolute URL for redirect (no payment state updates).
    /// </summary>
    Task<string> ProcessReturnAsync(IReadOnlyDictionary<string, string> queryParameters);
}
