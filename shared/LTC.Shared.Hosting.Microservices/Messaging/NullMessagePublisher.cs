namespace LTC.Shared.Hosting.Microservices.Messaging;

public sealed class NullMessagePublisher : IMessagePublisher
{
    public Task PublishAsync<TPayload>(string routingKey, string messageKey, TPayload payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
