using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Entities;
using LTC.PaymentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LTC.PaymentService.Services;

public class RefundAppService : ApplicationService, IRefundAppService
{
    private readonly IRepository<Refund, Guid> _refundRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;

    public RefundAppService(
        IRepository<Refund, Guid> refundRepository,
        IRepository<Payment, Guid> paymentRepository)
    {
        _refundRepository = refundRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<PagedResultDto<RefundOutputDto>> GetListAsync(GetRefundListInputDto input)
    {
        var queryable = await _refundRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Status))
        {
            queryable = queryable.Where(x => x.Status == input.Status);
        }

        var totalCount = await queryable.CountAsync();
        var items = await queryable.OrderBy(input.Sorting).PageBy(input.SkipCount, input.MaxResultCount).ToListAsync();

        return new PagedResultDto<RefundOutputDto>(
            totalCount,
            ObjectMapper.Map<List<Refund>, List<RefundOutputDto>>(items)
        );
    }

    public async Task<RefundOutputDto> GetAsync(Guid id)
    {
        var entity = await _refundRepository.GetAsync(id);
        return ObjectMapper.Map<Refund, RefundOutputDto>(entity);
    }

    public async Task<RefundOutputDto> InitiateRefundAsync(CreateRefundInputDto input)
    {
        var payment = await _paymentRepository.FirstOrDefaultAsync(x => x.BookingId == input.BookingId && x.PaymentStatus == "SUCCESS");
        if (payment == null)
        {
            throw new Exception("Payment not found or not successful for this booking");
        }

        var refund = new Refund
        {
            BookingId = input.BookingId,
            PaymentId = payment.Id,
            Amount = input.Amount,
            Reason = input.Reason,
            Status = "PENDING",    
            RequestedAt = DateTime.UtcNow
        };

        await _refundRepository.InsertAsync(refund);

        return ObjectMapper.Map<Refund, RefundOutputDto>(refund);
    }
}
