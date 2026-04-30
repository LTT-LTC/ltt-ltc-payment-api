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

public class PaymentAppService : ApplicationService, IPaymentAppService
{
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<PaymentAuditLog, Guid> _auditLogRepository;

    public PaymentAppService(
        IRepository<Payment, Guid> paymentRepository,
        IRepository<PaymentAuditLog, Guid> auditLogRepository)
    {
        _paymentRepository = paymentRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResultDto<PaymentOutputDto>> GetPaymentListAsync(GetPaymentListInputDto input)
    {
        var queryable = await _paymentRepository.GetQueryableAsync();

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

        return new PagedResultDto<PaymentOutputDto>(
            totalCount,
            ObjectMapper.Map<List<Payment>, List<PaymentOutputDto>>(items)
        );
    }

    public async Task<PaymentOutputDto> GetPaymentAsync(Guid id)
    {
        var entity = await _paymentRepository.GetAsync(id);
        return ObjectMapper.Map<Payment, PaymentOutputDto>(entity);
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
            ObjectMapper.Map<List<PaymentAuditLog>, List<PaymentAuditLogOutputDto>>(items)
        );
    }
}
