namespace OrderTracking.Application.Abstractions.Messaging;

public interface IOrderTrackingEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : notnull;
}
