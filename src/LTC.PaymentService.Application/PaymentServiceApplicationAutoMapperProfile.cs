using AutoMapper;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Entities;

namespace LTC.PaymentService;

public class PaymentServiceApplicationAutoMapperProfile : Profile
{
    public PaymentServiceApplicationAutoMapperProfile()
    {
        CreateMap<Payment, PaymentOutputDto>();
        CreateMap<PaymentAuditLog, PaymentAuditLogOutputDto>();
        CreateMap<PaymentRequest, PaymentRequestOutputDto>();
        CreateMap<Refund, RefundOutputDto>();
    }
}
