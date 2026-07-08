using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderTracking.Infrastructure.Messaging;

internal sealed class RabbitMqAnalyticsConsumer(
    IConnection rabbitMqConnection,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqAnalyticsConsumer> logger)
    : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = rabbitMqConnection.CreateModel();
        _channel.ExchangeDeclare(options.Value.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare("order-tracking.analytics", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("order-tracking.analytics", options.Value.ExchangeName, "*.changed");
        _channel.BasicQos(0, 20, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(args.Body.Span);
                LogAnalyticsEventConsumed(logger, args.RoutingKey, args.BasicProperties.MessageId, payload.Length, null);
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                await Task.CompletedTask;
            }
            catch (Exception exception)
            {
                LogAnalyticsProcessingFailed(logger, exception);
                _channel?.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume("order-tracking.analytics", autoAck: false, consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }

    private static readonly Action<ILogger, string, string, int, Exception?> LogAnalyticsEventConsumed =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Information,
            new EventId(3300, nameof(LogAnalyticsEventConsumed)),
            "Analytics event consumed. RoutingKey={RoutingKey}, MessageId={MessageId}, PayloadLength={PayloadLength}");

    private static readonly Action<ILogger, Exception?> LogAnalyticsProcessingFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3301, nameof(LogAnalyticsProcessingFailed)),
            "Analytics event processing failed.");
}
