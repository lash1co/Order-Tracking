using Microsoft.Extensions.Logging;
using OrderTracking.Application.Abstractions.Messaging;

namespace OrderTracking.Infrastructure.Messaging;

internal sealed class NoOpOrderTrackingEventPublisher(ILogger<NoOpOrderTrackingEventPublisher> logger)
    : IOrderTrackingEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : notnull
    {
        LogEventSkipped(logger, typeof(TEvent).Name, null);
        return Task.CompletedTask;
    }

    private static readonly Action<ILogger, string, Exception?> LogEventSkipped =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(3000, nameof(LogEventSkipped)),
            "Integration event {EventType} skipped because RabbitMQ is disabled.");
}
