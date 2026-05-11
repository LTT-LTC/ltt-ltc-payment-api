namespace LTC.Shared.Hosting.Microservices.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>When false, no broker connection is created and publishing is a no-op.</summary>
    public bool Enabled { get; set; } = true;

    public string HostName { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    /// <summary>Durable topic exchange; former Kafka topic names are used as routing keys.</summary>
    public string Exchange { get; set; } = "ltc";

    public RabbitMqRoutingKeys RoutingKeys { get; set; } = new();

    public RabbitMqConsumerOptions Consumer { get; set; } = new();
}

public class RabbitMqRoutingKeys
{
    public string BookingRequested { get; set; } = "ltc.booking.requested";

    public string BookingRequestedDlq { get; set; } = "ltc.booking.requested.dlq";

    public string SeatHoldEvents { get; set; } = "ltc.seat.holds";

    public string ShowtimeSeatMergeRequested { get; set; } = "ltc.showtime.seat.merge.requested";
}

public class RabbitMqConsumerOptions
{
    /// <summary>Queue for payment service to consume booking requests (bound to <see cref="RabbitMqRoutingKeys.BookingRequested"/>).</summary>
    public string BookingRequestedQueue { get; set; } = "ltc.payment.booking.requested";

    /// <summary>Dead-letter queue for failed BookingRequested messages after max retries.</summary>
    public string BookingRequestedDlqQueue { get; set; } = "ltc.payment.booking.requested.dlq";

    /// <summary>Queue for administration service to merge paid seats into showtime layout JSON.</summary>
    public string ShowtimeSeatMergeRequestedQueue { get; set; } = "ltc.administration.showtime.seat.merge";

    /// <summary>Dead-letter queue for failed ShowtimeSeatMergeRequested messages after max retries.</summary>
    public string ShowtimeSeatMergeDlqQueue { get; set; } = "ltc.administration.showtime.seat.merge.dlq";
}
