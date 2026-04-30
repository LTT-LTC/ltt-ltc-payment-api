using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using LTC.Shared.Hosting.Microservices.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LTC.PaymentService.Services.Kafka;

public class BookingRequestedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<BookingRequestedConsumer> _logger;
    private readonly ConcurrentDictionary<Guid, byte> _processedBookingIds = new();

    public BookingRequestedConsumer(IOptions<KafkaOptions> kafkaOptions, ILogger<BookingRequestedConsumer> logger)
    {
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken cancellationToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = _kafkaOptions.Consumer.GroupId,
            AutoOffsetReset = _kafkaOptions.Consumer.AutoOffsetReset.Equals("Earliest", StringComparison.OrdinalIgnoreCase)
                ? Confluent.Kafka.AutoOffsetReset.Earliest
                : Confluent.Kafka.AutoOffsetReset.Latest,
            EnableAutoCommit = _kafkaOptions.Consumer.EnableAutoCommit
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_kafkaOptions.Topics.BookingRequested);
        _logger.LogInformation("BookingRequested consumer started on topic {Topic}", _kafkaOptions.Topics.BookingRequested);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(cancellationToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                var payload = JsonSerializer.Deserialize<BookingRequestedEvent>(result.Message.Value, JsonSerializerOptions);
                if (payload is null)
                {
                    _logger.LogWarning("Skip invalid BookingRequested payload at offset {Offset}", result.Offset.Value);
                    consumer.Commit(result);
                    continue;
                }

                // Basic in-memory idempotency guard for repeated Kafka deliveries on single instance.
                if (!_processedBookingIds.TryAdd(payload.BookingId, 1))
                {
                    _logger.LogInformation("Skip duplicate BookingRequested for booking {BookingId}", payload.BookingId);
                    consumer.Commit(result);
                    continue;
                }

                _logger.LogInformation(
                    "BookingRequested received. BookingId={BookingId}, TenantId={TenantId}, UserId={UserId}, TotalPrice={TotalPrice}, CorrelationId={CorrelationId}",
                    payload.BookingId,
                    payload.TenantId,
                    payload.UserId,
                    payload.TotalPrice,
                    payload.CorrelationId);

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume failure for BookingRequested topic.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected BookingRequested consumer error.");
            }
        }

        consumer.Close();
    }
}
