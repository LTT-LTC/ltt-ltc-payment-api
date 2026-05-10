using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LTC.Shared.Hosting.Microservices.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace LTC.PaymentService.Services.Messaging;

public class BookingRequestedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<Guid, byte> _processedBookingIds = new();

    private readonly RabbitMqOptions _options;
    private readonly ILogger<BookingRequestedConsumer> _logger;

    public BookingRequestedConsumer(IOptions<RabbitMqOptions> options, ILogger<BookingRequestedConsumer> logger)
    {
        _options = options.Value;
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
            channel.QueueDeclare(
                queue: _options.Consumer.BookingRequestedQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            channel.QueueBind(
                queue: _options.Consumer.BookingRequestedQueue,
                exchange: _options.Exchange,
                routingKey: _options.RoutingKeys.BookingRequested,
                arguments: null);
            channel.BasicQos(0, 1, false);

            var consumer = new global::RabbitMQ.Client.Events.AsyncEventingBasicConsumer(channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var payload = JsonSerializer.Deserialize<BookingRequestedEvent>(json, JsonSerializerOptions);
                    if (payload is null)
                    {
                        _logger.LogWarning("Skip invalid BookingRequested payload");
                        channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    if (!_processedBookingIds.TryAdd(payload.BookingId, 1))
                    {
                        _logger.LogInformation("Skip duplicate BookingRequested for booking {BookingId}", payload.BookingId);
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
                    _logger.LogError(ex, "RabbitMQ consume failure for BookingRequested queue.");
                    try
                    {
                        channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                    }
                    catch (Exception nackEx)
                    {
                        _logger.LogError(nackEx, "Failed to nack message.");
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
}
