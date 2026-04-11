using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;







namespace LTC.PaymentService.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class PaymentServiceDbContext :
    AbpDbContext<PaymentServiceDbContext>
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    public PaymentServiceDbContext(DbContextOptions<PaymentServiceDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigureAuditLogging();

        /* Configure your own tables/entities inside here */

        //builder.Entity<YourEntity>(b =>
        //{
        //    b.ToTable(PaymentServiceConsts.DbTablePrefix + "YourEntities", PaymentServiceConsts.DbSchema);
        //    b.ConfigureByConvention(); //auto configure for the base class props
        //    //...
        //});
    }
}
