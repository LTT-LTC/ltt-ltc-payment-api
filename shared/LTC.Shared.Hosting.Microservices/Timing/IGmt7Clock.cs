using System;

namespace LTC.Shared.Hosting.Microservices.Timing;

public interface IGmt7Clock
{
    DateTime UtcNow { get; }
    DateTime Gmt7Now { get; }
}
