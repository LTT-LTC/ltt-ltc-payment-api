using System;
using Volo.Abp.Domain.Entities;

namespace LTC.PaymentService.Entities;

/// <summary>
/// Host dbo lookup so anonymous VNPAY IPN can resolve tenant/schema before loading <see cref="PaymentRequest"/>.
/// </summary>
public class VnpayTxnRouting : Entity<Guid>
{
    protected VnpayTxnRouting()
    {
    }

    public VnpayTxnRouting(Guid id)
        : base(id)
    {
    }

    public Guid TenantId { get; set; }

    /// <summary>
    /// Tenant name used as SQL schema name (same as <c>X-Tenant</c> / <see cref="Volo.Abp.MultiTenancy.ICurrentTenant.Name"/>).
    /// </summary>
    public string TenantName { get; set; } = default!;
}
