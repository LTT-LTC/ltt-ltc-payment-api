using Microsoft.EntityFrameworkCore;
using LTC.PaymentService.Entities;
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

    public PaymentServiceDbContext(DbContextOptions<PaymentServiceDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(PaymentServiceConsts.DbSchema);

        /* Include modules to your migration db context */

        builder.ConfigureAuditLogging();

        builder.Entity<Payment>(b =>
        {
            b.ToTable("Payments", PaymentServiceConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });

        builder.Entity<PaymentRequest>(b =>
        {
            b.ToTable("PaymentRequests", PaymentServiceConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });

        builder.Entity<PaymentAuditLog>(b =>
        {
            b.ToTable("PaymentAuditLogs", PaymentServiceConsts.DbSchema);
            b.ConfigureByConvention();
        });

        builder.Entity<Refund>(b =>
        {
            b.ToTable("Refunds", PaymentServiceConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });
    }
}
