using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderTracking.Application.Abstractions.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace OrderTracking.Infrastructure.Messaging;

internal sealed class RabbitMqOrderTrackingEventPublisher(
    IConnection rabbitMqConnection,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqOrderTrackingEventPublisher> logger)
    : IOrderTrackingEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : notnull
    {
        try
        {
            using var channel = rabbitMqConnection.CreateModel();
            channel.ExchangeDeclare(options.Value.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

            var eventType = typeof(TEvent).Name;
            var envelope = new IntegrationEventEnvelope<TEvent>(
                Guid.NewGuid(),
                eventType,
                DateTimeOffset.UtcNow,
                integrationEvent);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, JsonOptions));
            var properties = channel.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2;
            properties.MessageId = envelope.Id.ToString();
            properties.Type = eventType;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                options.Value.ExchangeName,
                RoutingKey.For(eventType),
                mandatory: false,
                basicProperties: properties,
                body: body);
        }
        catch (Exception exception) when (exception is BrokerUnreachableException or AlreadyClosedException or OperationInterruptedException)
        {
            LogPublishFailed(logger, typeof(TEvent).Name, exception);
        }

        return Task.CompletedTask;
    }

    private sealed record IntegrationEventEnvelope<TPayload>(
        Guid Id,
        string Type,
        DateTimeOffset OccurredAt,
        TPayload Payload);

    private static class RoutingKey
    {
        public static string For(string eventType) => eventType
            .Replace("Event", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Changed", ".changed", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();
    }

    private static readonly Action<ILogger, string, Exception?> LogPublishFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(3200, nameof(LogPublishFailed)),
            "RabbitMQ publish failed for {EventType}.");
}
