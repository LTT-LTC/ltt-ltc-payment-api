namespace LTC.Shared.Hosting.Microservices.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<TPayload>(string routingKey, string messageKey, TPayload payload, CancellationToken cancellationToken = default);
}
