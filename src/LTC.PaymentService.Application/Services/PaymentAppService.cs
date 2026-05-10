using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using LTC.PaymentService.Dtos.Input;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Entities;
using LTC.PaymentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LTC.PaymentService.Services;

public class PaymentAppService : ApplicationService, IPaymentAppService
{
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<PaymentRequest, Guid> _paymentRequestRepository;
    private readonly IRepository<PaymentAuditLog, Guid> _auditLogRepository;

    public PaymentAppService(
        IRepository<Payment, Guid> paymentRepository,
        IRepository<PaymentRequest, Guid> paymentRequestRepository,
        IRepository<PaymentAuditLog, Guid> auditLogRepository)
    {
        _paymentRepository = paymentRepository;
        _paymentRequestRepository = paymentRequestRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResultDto<PaymentOutputDto>> GetPaymentListAsync(GetPaymentListInputDto input)
    {
        var queryable = await _paymentRepository.GetQueryableAsync();

        if (input.CustomerId.HasValue)
        {
            var requestQueryable = await _paymentRequestRepository.GetQueryableAsync();
            var ownedIds = requestQueryable
                .Where(pr => pr.CustomerId == input.CustomerId.Value)
                .Select(pr => pr.Id);
            queryable = queryable.Where(p => ownedIds.Contains(p.PaymentRequestId));
        }

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

    public async Task<PagedResultDto<PaymentOutputDto>> GetMyPaymentsAsync(GetPaymentListInputDto input)
    {
        if (!CurrentUser.IsAuthenticated || !CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Authentication required.");
        }

        var customerId = CurrentUser.GetId();
        var queryable = await _paymentRepository.GetQueryableAsync();
        var requestQueryable = await _paymentRequestRepository.GetQueryableAsync();

        var ownedRequests = requestQueryable.Where(pr => pr.CustomerId == customerId);
        if (CurrentTenant.Id.HasValue)
        {
            ownedRequests = ownedRequests.Where(pr => pr.TenantId == CurrentTenant.Id);
        }

        var ownedRequestIds = ownedRequests.Select(pr => pr.Id);
        queryable = queryable.Where(p => ownedRequestIds.Contains(p.PaymentRequestId));

        if (CurrentTenant.Id.HasValue)
        {
            queryable = queryable.Where(p => p.TenantId == CurrentTenant.Id);
        }

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

    public async Task<PaymentOutputDto> GetMyPaymentAsync(Guid id)
    {
        if (!CurrentUser.IsAuthenticated || !CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Authentication required.");
        }

        var customerId = CurrentUser.GetId();
        var entity = await _paymentRepository.GetAsync(id);
        var request = await _paymentRequestRepository.GetAsync(entity.PaymentRequestId);

        if (request.CustomerId != customerId)
        {
            throw new AbpAuthorizationException("You do not have access to this payment.");
        }

        if (CurrentTenant.Id.HasValue &&
            (entity.TenantId != CurrentTenant.Id || request.TenantId != CurrentTenant.Id))
        {
            throw new AbpAuthorizationException("You do not have access to this payment.");
        }

        return ObjectMapper.Map<Payment, PaymentOutputDto>(entity);
    }
}
