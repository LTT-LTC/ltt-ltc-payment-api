using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace LTC.Shared.Hosting.Microservices.Messaging;

/// <summary>
/// Publishes via RabbitMQ using a lazily opened connection. If the broker is unreachable,
/// errors are logged and the host keeps running (publish becomes a no-op for that attempt).
/// </summary>
public sealed class RabbitMqMessagePublisher : IMessagePublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly object _connectionLock = new();
    private global::RabbitMQ.Client.IConnection? _connection;
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;

    public RabbitMqMessagePublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqMessagePublisher> logger)
    {
        _options = options;
        _logger = logger;
    }

    public void Dispose()
    {
        lock (_connectionLock)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }

    public Task PublishAsync<TPayload>(string routingKey, string messageKey, TPayload payload, CancellationToken cancellationToken = default)
    {
        var conn = TryGetOpenConnection();
        if (conn is null)
        {
            return Task.CompletedTask;
        }

        var opt = _options.Value;
        try
        {
            var json = JsonSerializer.Serialize(payload, JsonSerializerOptions);
            var body = Encoding.UTF8.GetBytes(json);

            using var channel = conn.CreateModel();
            channel.ExchangeDeclare(
                exchange: opt.Exchange,
                type: global::RabbitMQ.Client.ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null);

            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.MessageId = messageKey;

            channel.BasicPublish(
                exchange: opt.Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);

            _logger.LogInformation(
                "RabbitMQ message published. Exchange={Exchange} RoutingKey={RoutingKey} MessageId={MessageId}",
                opt.Exchange,
                routingKey,
                messageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "RabbitMQ publish failed (broker unreachable or channel error). Exchange={Exchange} RoutingKey={RoutingKey}",
                opt.Exchange,
                routingKey);
            InvalidateConnection();
        }

        return Task.CompletedTask;
    }

    private global::RabbitMQ.Client.IConnection? TryGetOpenConnection()
    {
        lock (_connectionLock)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            try
            {
                _connection?.Dispose();
                var o = _options.Value;
                var factory = new global::RabbitMQ.Client.ConnectionFactory
                {
                    HostName = o.HostName,
                    Port = o.Port > 0 ? o.Port : 5672,
                    VirtualHost = string.IsNullOrEmpty(o.VirtualHost) ? "/" : o.VirtualHost,
                    UserName = string.IsNullOrEmpty(o.UserName) ? "guest" : o.UserName,
                    Password = o.Password ?? "guest",
                };
                _connection = factory.CreateConnection();
                return _connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Cannot connect to RabbitMQ at {HostName}:{Port}; application continues without messaging.",
                    _options.Value.HostName,
                    _options.Value.Port > 0 ? _options.Value.Port : 5672);
                _connection = null;
                return null;
            }
        }
    }

    private void InvalidateConnection()
    {
        lock (_connectionLock)
        {
            try
            {
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing RabbitMQ connection after publish failure.");
            }

            _connection = null;
        }
    }
}
