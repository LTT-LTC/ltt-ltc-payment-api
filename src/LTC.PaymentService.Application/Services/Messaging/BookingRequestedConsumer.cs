using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LTC.Shared.Hosting.Microservices.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LTC.PaymentService.Services.Messaging;

public class BookingRequestedConsumer : BackgroundService
{
    private const int MaxRetryCount = 3;
    private const int RetryDelaySeconds = 30;
    private const string DedupKeyPrefix = "dedup:booking-requested:";
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<BookingRequestedConsumer> _logger;

    public BookingRequestedConsumer(
        IOptions<RabbitMqOptions> options,
        IConnectionMultiplexer redis,
        ILogger<BookingRequestedConsumer> logger)
    {
        _options = options.Value;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.HostName))
        {
            _logger.LogWarning(
                "BookingRequested consumer skipped (RabbitMq:Enabled=false or HostName empty).");
            return;
        }

        _logger.LogInformation(
            "BookingRequested consumer starting with HostName={HostName} Queue={Queue}",
            _options.HostName,
            _options.Consumer.BookingRequestedQueue);

        var factory = new global::RabbitMQ.Client.ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port > 0 ? _options.Port : 5672,
            VirtualHost = string.IsNullOrEmpty(_options.VirtualHost) ? "/" : _options.VirtualHost,
            UserName = string.IsNullOrEmpty(_options.UserName) ? "guest" : _options.UserName,
            Password = _options.Password ?? "guest",
        };

        global::RabbitMQ.Client.IConnection connection;
        try
        {
            connection = factory.CreateConnection();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "BookingRequested consumer cannot connect to RabbitMQ at {HostName}:{Port}; payment API continues without consuming booking messages.",
                _options.HostName,
                _options.Port > 0 ? _options.Port : 5672);
            return;
        }

        using (connection)
        {
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: _options.Exchange,
                type: global::RabbitMQ.Client.ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null);

            // Main queue — attempt to declare with DLX args for new queues.
            // Falls back to plain declare if queue already exists without DLX args.
            DeclareQueueWithDlxFallback(
                connection,
                queue: _options.Consumer.BookingRequestedQueue,
                exchange: _options.Exchange,
                dlxRoutingKey: _options.RoutingKeys.BookingRequestedDlq);
            channel.QueueBind(
                queue: _options.Consumer.BookingRequestedQueue,
                exchange: _options.Exchange,
                routingKey: _options.RoutingKeys.BookingRequested,
                arguments: null);

            // DLQ — terminal; no further routing
            channel.QueueDeclare(
                queue: _options.Consumer.BookingRequestedDlqQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            channel.QueueBind(
                queue: _options.Consumer.BookingRequestedDlqQueue,
                exchange: _options.Exchange,
                routingKey: _options.RoutingKeys.BookingRequestedDlq,
                arguments: null);

            channel.BasicQos(0, 1, false);

            var consumer = new global::RabbitMQ.Client.Events.AsyncEventingBasicConsumer(channel);
            consumer.Received += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var messageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString("N");
                var retryCount = GetRetryCount(ea.BasicProperties);

                try
                {
                    var payload = JsonSerializer.Deserialize<BookingRequestedEvent>(json, JsonSerializerOptions);
                    if (payload is null)
                    {
                        _logger.LogWarning("Skip invalid BookingRequested payload");
                        channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    // Redis-based distributed deduplication (TTL 24h, safe across multiple instances)
                    var db = _redis.GetDatabase();
                    var dedupKey = $"{DedupKeyPrefix}{payload.BookingId}";
                    var isNew = await db.StringSetAsync(dedupKey, "1", TimeSpan.FromHours(24), When.NotExists);
                    if (!isNew)
                    {
                        _logger.LogInformation(
                            "Skip duplicate BookingRequested for booking {BookingId}", payload.BookingId);
                        channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    _logger.LogInformation(
                        "BookingRequested received. BookingId={BookingId}, TenantId={TenantId}, UserId={UserId}, TotalPrice={TotalPrice}, CorrelationId={CorrelationId}",
                        payload.BookingId,
                        payload.TenantId,
                        payload.UserId,
                        payload.TotalPrice,
                        payload.CorrelationId);

                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "RabbitMQ consume failure for BookingRequested. MessageId={MessageId} RetryCount={RetryCount}",
                        messageId,
                        retryCount);

                    if (retryCount < MaxRetryCount)
                    {
                        // Fire-and-forget: wait RetryDelaySeconds then re-publish with incremented retry counter
                        var capturedJson = json;
                        var capturedMessageId = messageId;
                        var capturedRetry = retryCount + 1;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds));
                                RepublishToMainQueue(capturedJson, capturedMessageId, capturedRetry);
                            }
                            catch (Exception retryEx)
                            {
                                _logger.LogError(
                                    retryEx,
                                    "Background retry failed for BookingRequested MessageId={MessageId}",
                                    capturedMessageId);
                            }
                        });
                        _logger.LogWarning(
                            "Scheduled retry {NextRetry}/{Max} for MessageId={MessageId} in {Delay}s",
                            retryCount + 1, MaxRetryCount, messageId, RetryDelaySeconds);
                        // Ack the original so it's removed; the retry task re-publishes it
                        try { channel.BasicAck(ea.DeliveryTag, false); } catch { /* channel may be closing */ }
                    }
                    else
                    {
                        // Exhausted retries — nack without requeue; DLX routes to DLQ
                        _logger.LogError(
                            "BookingRequested MessageId={MessageId} exhausted {Max} retries; routing to DLQ",
                            messageId, MaxRetryCount);
                        try
                        {
                            channel.BasicNack(ea.DeliveryTag, false, requeue: false);
                        }
                        catch (Exception nackEx)
                        {
                            _logger.LogError(nackEx, "Failed to nack message.");
                        }
                    }
                }
            };

            channel.BasicConsume(
                queue: _options.Consumer.BookingRequestedQueue,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // shutdown
            }
        }
    }

    private void DeclareQueueWithDlxFallback(
        global::RabbitMQ.Client.IConnection connection,
        string queue,
        string exchange,
        string dlxRoutingKey)
    {
        try
        {
            using var ch = connection.CreateModel();
            ch.QueueDeclare(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = exchange,
                    ["x-dead-letter-routing-key"] = dlxRoutingKey,
                });
            return;
        }
        catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
            when (ex.ShutdownReason?.ReplyCode == 406)
        {
            _logger.LogWarning(
                "Queue '{Queue}' already exists without DLX arguments. " +
                "DLQ routing will not be active until the queue is deleted and recreated. " +
                "Continuing with existing queue.",
                queue);
        }

        using var fallbackCh = connection.CreateModel();
        fallbackCh.QueueDeclare(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    private void RepublishToMainQueue(string messageJson, string messageId, int retryCount)
    {
        var factory = new global::RabbitMQ.Client.ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port > 0 ? _options.Port : 5672,
            VirtualHost = string.IsNullOrEmpty(_options.VirtualHost) ? "/" : _options.VirtualHost,
            UserName = string.IsNullOrEmpty(_options.UserName) ? "guest" : _options.UserName,
            Password = _options.Password ?? "guest",
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: _options.Exchange,
            type: global::RabbitMQ.Client.ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(messageJson);
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.MessageId = messageId;
        props.Headers = new Dictionary<string, object> { ["x-retry-count"] = retryCount };

        channel.BasicPublish(
            exchange: _options.Exchange,
            routingKey: _options.RoutingKeys.BookingRequested,
            mandatory: false,
            basicProperties: props,
            body: body);

        _logger.LogInformation(
            "RepublishToMainQueue: re-published MessageId={MessageId} RetryCount={RetryCount}",
            messageId,
            retryCount);
    }

    private static int GetRetryCount(global::RabbitMQ.Client.IBasicProperties? props)
    {
        if (props?.Headers == null) return 0;
        if (!props.Headers.TryGetValue("x-retry-count", out var raw)) return 0;
        return raw switch
        {
            int i => i,
            long l => (int)l,
            byte[] b => b.Length > 0 ? b[0] : 0,
            _ => 0
        };
    }
}
