using System.Threading.Tasks;

namespace LTC.PaymentService.Data;

public interface IPaymentServiceDbSchemaMigrator
{
    Task MigrateAsync();
}
