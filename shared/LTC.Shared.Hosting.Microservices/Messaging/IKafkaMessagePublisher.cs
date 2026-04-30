namespace LTC.Shared.Hosting.Microservices.Messaging;

public interface IKafkaMessagePublisher
{
    Task PublishAsync<TPayload>(string topic, string key, TPayload payload, CancellationToken cancellationToken = default);
}
