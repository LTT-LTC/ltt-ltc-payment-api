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

public class PaymentRequestAppService : ApplicationService, IPaymentRequestAppService
{
    private readonly IRepository<PaymentRequest, Guid> _repository;

    public PaymentRequestAppService(IRepository<PaymentRequest, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<PaymentRequestOutputDto>> GetPaymentRequestListAsync(GetPaymentRequestListInputDto input)
    {
        var queryable = await _repository.GetQueryableAsync();

        if (input.FromDate.HasValue)
        {
            queryable = queryable.Where(x => x.CreatedAt >= input.FromDate.Value);
        }
        if (input.ToDate.HasValue)
        {
            queryable = queryable.Where(x => x.CreatedAt <= input.ToDate.Value);
        }

        var totalCount = await queryable.CountAsync();
        var items = await queryable.OrderBy(input.Sorting).PageBy(input.SkipCount, input.MaxResultCount).ToListAsync();

        return new PagedResultDto<PaymentRequestOutputDto>(
            totalCount,
            ObjectMapper.Map<List<PaymentRequest>, List<PaymentRequestOutputDto>>(items)
        );
    }

    public async Task<PaymentRequestOutputDto> GetPaymentRequestAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        return ObjectMapper.Map<PaymentRequest, PaymentRequestOutputDto>(entity);
    }
}
