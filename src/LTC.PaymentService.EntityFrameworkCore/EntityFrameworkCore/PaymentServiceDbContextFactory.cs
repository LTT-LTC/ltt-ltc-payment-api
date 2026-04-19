using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LTC.PaymentService.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class PaymentServiceDbContextFactory : IDesignTimeDbContextFactory<PaymentServiceDbContext>
{
    public PaymentServiceDbContext CreateDbContext(string[] args)
    {
        PaymentServiceEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<PaymentServiceDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new PaymentServiceDbContext(builder.Options);
    }



    private static IConfigurationRoot BuildConfiguration()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidateBasePaths = new[]
        {
            Path.Combine(currentDirectory, "../LTC.PaymentService.DbMigrator/"),
            Path.Combine(currentDirectory, "../src/LTC.PaymentService.DbMigrator/"),
            Path.Combine(currentDirectory, "../../src/LTC.PaymentService.DbMigrator/")
        };

        var dbMigratorPath = candidateBasePaths
            .Select(Path.GetFullPath)
            .FirstOrDefault(Directory.Exists);

        if (dbMigratorPath == null)
        {
            throw new DirectoryNotFoundException("Unable to locate LTC.PaymentService.DbMigrator directory for EF design-time configuration.");
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(dbMigratorPath)
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
