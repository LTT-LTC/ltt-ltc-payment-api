using System.Collections.Generic;
using System.Threading.Tasks;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using Volo.Abp.Application.Services;
using System;

namespace LTC.PaymentService.Interfaces;

public interface IVnPayAppService : IApplicationService
{
    Task<CreateVnPayPaymentUrlOutputDto> CreatePaymentUrlAsync(
        CreateVnPayPaymentUrlInputDto input,
        string clientIpAddress);

    Task<VnPayIpnResponseDto> ProcessIpnAsync(IReadOnlyDictionary<string, string> queryParameters);

    /// <summary>
    /// Browser return URL: verifies signature, redirects to frontend, and applies the same SUCCESS/FAILED
    /// update as IPN when the payment row is still <c>PENDING</c> (covers environments where IPN cannot reach the server).
    /// </summary>
    Task<string> ProcessReturnAsync(IReadOnlyDictionary<string, string> queryParameters);

    /// <summary>
    /// Manually check and complete a VNPay payment by booking ID.
    /// This is a fallback for when IPN and browser return fail to update the booking status.
    /// Verifies the payment status with VNPay and completes the booking workflow if payment was successful.
    /// </summary>
    Task<ManualVnpayCompletionOutputDto> ManualCompleteByBookingAsync(Guid bookingId);
}
