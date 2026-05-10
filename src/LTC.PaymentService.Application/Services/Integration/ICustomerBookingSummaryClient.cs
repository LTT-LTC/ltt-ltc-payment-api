using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace LTC.PaymentService.Services.Integration;

public interface ICustomerBookingSummaryClient : ITransientDependency
{
    /// <summary>
    /// Loads booking rows from customer-service internal API.
    /// Returns empty map when integration is not configured or the call fails.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, BookingPaymentSummaryRow>> GetSummariesAsync(
        IReadOnlyList<Guid> bookingIds,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
