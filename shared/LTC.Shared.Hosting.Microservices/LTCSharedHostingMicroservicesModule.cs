using Hangfire;
using Hangfire.RecurringJobAdmin;
using Hangfire.Redis.StackExchange;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.AspNetCore.Authentication.JwtBearer;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.BackgroundWorkers.Hangfire;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Guids;
using Volo.Abp.Modularity;

namespace LTC.Shared.Hosting.Microservices
{
    [DependsOn(
        typeof(AbpAspNetCoreAuthenticationJwtBearerModule),
        typeof(AbpCachingStackExchangeRedisModule),
        //typeof(AdministrationServiceEntityFrameworkCoreModule),
        typeof(AbpDistributedLockingModule)
    //
    //typeof(AbpBackgroundWorkersModule),
    //
    //typeof(AbpBackgroundWorkersHangfireModule)
    )]
    public class LTCSharedHostingMicroservicesModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //context.Services.AddMonitoringServices();
            var configuration = context.Services.GetConfiguration();
            var environment = context.Services.GetHostingEnvironment();
            Configure<AbpDistributedCacheOptions>(options =>
            {
                options.KeyPrefix = $"{environment.ApplicationName}:";
            });

            IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]!);
            context.Services.AddSingleton(connectionMultiplexer);
            context.Services.AddDataProtection().PersistKeysToStackExchangeRedis(connectionMultiplexer, $"{environment.ApplicationName}-ProtectionKeys");
            context.Services.AddSingleton<IDistributedLockProvider>(sp =>
            {
                return new RedisDistributedSynchronizationProvider(connectionMultiplexer.GetDatabase());
            });

            ConfigureHangfire(context, configuration, environment, connectionMultiplexer);

        }

        private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration, IWebHostEnvironment environment, IConnectionMultiplexer connectionMultiplexer, string prefix = "Hangfire:")
        {
            var isEnabledWorker = configuration.GetValue<bool?>("BackgroundJob:IsEnabledWorker") ?? false;
            Configure<AbpBackgroundWorkerOptions>(options =>
            {
                options.IsEnabled = isEnabledWorker;
            });
            Configure<AbpBackgroundJobWorkerOptions>(options =>
            {
                options.DefaultTimeout = 0;
            });

            var backgroundJobsStorageProvider = configuration.GetValue<string>("BackgroundJob:StorageProvider") ?? "SqlServer";
            var backgroundJobsStorageConnectionStringName = configuration.GetValue<string>("BackgroundJob:StorageConnectionStringName") ?? "SqlServer";

            if (backgroundJobsStorageProvider.ToUpper() == "REDIS")
            {
                context.Services.AddHangfire(config =>
                {
                    config.UseFilter(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = [60, 120, 240] });
                    config.UseRedisStorage(configuration.GetValue<string>("Redis:Configuration"),
                        new RedisStorageOptions
                        {
                            Prefix = $"{environment.ApplicationName}-{prefix}:",
                            Db = connectionMultiplexer.GetDatabase().Database,
                        }).UseRecurringJobAdmin(typeof(LTCSharedHostingMicroservicesModule).Assembly);
                });
            }
        }
    }
}
