using Microsoft.EntityFrameworkCore;
using LTC.PaymentService.Entities;
using LTC.PaymentService.MultiTenancy;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;







namespace LTC.PaymentService.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class PaymentServiceDbContext :
    AbpDbContext<PaymentServiceDbContext>
{
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentRequest> PaymentRequests { get; set; }
    public DbSet<PaymentAuditLog> PaymentAuditLogs { get; set; }
    public DbSet<Refund> Refunds { get; set; }

    private readonly ITenantSchemaResolver? _tenantSchemaResolver;

    public PaymentServiceDbContext(
        DbContextOptions<PaymentServiceDbContext> options,
        ITenantSchemaResolver? tenantSchemaResolver = null)
        : base(options)
    {
        _tenantSchemaResolver = tenantSchemaResolver;
    }

    public string GetCurrentSchema() => _tenantSchemaResolver?.GetSchemaName() ?? "dbo";

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        var schema = GetCurrentSchema();

        // ABP audit logging tables use the default dbo schema — set it before calling ConfigureAuditLogging.
        builder.HasDefaultSchema("dbo");
        builder.ConfigureAuditLogging();

        // Reset to tenant schema so all service-specific entities use the correct per-tenant schema.
        builder.HasDefaultSchema(schema);

        builder.Entity<Payment>(b =>
        {
            b.ToTable("Payments");
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });

        builder.Entity<PaymentRequest>(b =>
        {
            b.ToTable("PaymentRequests");
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });

        builder.Entity<PaymentAuditLog>(b =>
        {
            b.ToTable("PaymentAuditLogs");
            b.ConfigureByConvention();
        });

        builder.Entity<Refund>(b =>
        {
            b.ToTable("Refunds");
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });
    }
}
