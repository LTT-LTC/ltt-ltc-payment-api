using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace LTC.Shared.Hosting.Microservices.Messaging;

public sealed class KafkaMessagePublisher : IKafkaMessagePublisher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaMessagePublisher> _logger;

    public KafkaMessagePublisher(IProducer<string, string> producer, ILogger<KafkaMessagePublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task PublishAsync<TPayload>(string topic, string key, TPayload payload, CancellationToken cancellationToken = default)
    {
        var value = JsonSerializer.Serialize(payload, JsonSerializerOptions);
        var message = new Message<string, string>
        {
            Key = key,
            Value = value
        };

        var result = await _producer.ProduceAsync(topic, message, cancellationToken);
        _logger.LogInformation("Kafka message published. Topic={Topic}, Partition={Partition}, Offset={Offset}, Key={Key}",
            result.Topic, result.Partition.Value, result.Offset.Value, key);
    }
}
