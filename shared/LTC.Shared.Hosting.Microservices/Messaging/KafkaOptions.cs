using System.ComponentModel.DataAnnotations;

namespace LTC.Shared.Hosting.Microservices.Messaging;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    [Required]
    public string BootstrapServers { get; set; } = string.Empty;

    public KafkaTopicOptions Topics { get; set; } = new();

    public KafkaConsumerOptions Consumer { get; set; } = new();

    public KafkaProducerOptions Producer { get; set; } = new();
}

public class KafkaTopicOptions
{
    public string BookingRequested { get; set; } = "ltc.booking.requested";

    public string BookingRequestedDlq { get; set; } = "ltc.booking.requested.dlq";
}

public class KafkaConsumerOptions
{
    public string GroupId { get; set; } = "ltc-consumer";

    public string AutoOffsetReset { get; set; } = "Earliest";

    public bool EnableAutoCommit { get; set; }
}

public class KafkaProducerOptions
{
    public string Acks { get; set; } = "all";

    public bool EnableIdempotence { get; set; } = true;
}
