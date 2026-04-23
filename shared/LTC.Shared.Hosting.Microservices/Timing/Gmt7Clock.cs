using System;
using Volo.Abp.DependencyInjection;

namespace LTC.Shared.Hosting.Microservices.Timing;

public class Gmt7Clock : IGmt7Clock, ITransientDependency
{
    private static readonly TimeZoneInfo Gmt7TimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "GMT+7",
        TimeSpan.FromHours(7),
        "GMT+7",
        "GMT+7");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Gmt7Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, Gmt7TimeZone);
}
