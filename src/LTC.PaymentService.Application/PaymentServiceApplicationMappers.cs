using System.Collections.Generic;
using LTC.PaymentService.Dtos.Output;
using LTC.PaymentService.Entities;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace LTC.PaymentService;

[Mapper]
public partial class PaymentServiceApplicationMappers
{
    public partial PaymentOutputDto Map(Payment source);

    public partial PaymentRequestOutputDto Map(PaymentRequest source);

    public partial PaymentAuditLogOutputDto Map(PaymentAuditLog source);

    public partial RefundOutputDto Map(Refund source);

    public partial List<PaymentOutputDto> Map(List<Payment> source);

    public partial List<PaymentAuditLogOutputDto> Map(List<PaymentAuditLog> source);

    public partial List<RefundOutputDto> Map(List<Refund> source);

    public partial List<PaymentRequestOutputDto> Map(List<PaymentRequest> source);
}
