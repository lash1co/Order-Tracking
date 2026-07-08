using Microsoft.Extensions.Options;
using OrderTracking.Application.Abstractions.Messaging;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Application.Drivers;
using OrderTracking.Application.Events;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.API.Simulation;

public sealed class DriverMovementSimulator(
    IServiceScopeFactory scopeFactory,
    IOptions<DriverMovementOptions> options,
    ILogger<DriverMovementSimulator> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            LogSimulatorDisabled(logger, null);
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervalSeconds)));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            await SimulateBatchAsync(stoppingToken);
    }

    private async Task SimulateBatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var drivers = scope.ServiceProvider.GetRequiredService<IDriverRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var notifier = scope.ServiceProvider.GetRequiredService<ITrackingNotifier>();
            var publisher = scope.ServiceProvider.GetRequiredService<IOrderTrackingEventPublisher>();

            var activeDrivers = await drivers.GetForLocationSimulationAsync(options.Value.BatchSize, cancellationToken);
            foreach (var driver in activeDrivers)
            {
                var location = Move(driver.CurrentLocation);
                driver.UpdateLocation(location);
            }

            if (activeDrivers.Count == 0)
                return;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var occurredAt = DateTimeOffset.UtcNow;
            foreach (var driver in activeDrivers)
            {
                await notifier.DriverLocationChangedAsync(DriverLocationDto.From(driver, occurredAt), cancellationToken);
                await publisher.PublishAsync(
                    new DriverLocationChangedEvent(
                        driver.Id,
                        driver.CurrentLocation.Latitude,
                        driver.CurrentLocation.Longitude,
                        driver.Status.ToString(),
                        occurredAt),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogSimulationFailed(logger, exception);
        }
    }

    private GeoLocation Move(GeoLocation current)
    {
        var delta = Math.Abs(options.Value.MaxDeltaDegrees);
        var latitude = current.Latitude + Random.Shared.NextDouble() * delta * 2 - delta;
        var longitude = current.Longitude + Random.Shared.NextDouble() * delta * 2 - delta;
        return GeoLocation.Create(Math.Clamp(latitude, -90, 90), Math.Clamp(longitude, -180, 180));
    }

    private static readonly Action<ILogger, Exception?> LogSimulatorDisabled =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2000, nameof(LogSimulatorDisabled)),
            "Driver movement simulator is disabled.");

    private static readonly Action<ILogger, Exception?> LogSimulationFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2001, nameof(LogSimulationFailed)),
            "Driver movement simulation failed; next tick will retry.");
}
