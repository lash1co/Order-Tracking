namespace OrderTracking.API.Simulation;

public sealed class DriverMovementOptions
{
    public bool Enabled { get; init; }
    public int IntervalSeconds { get; init; } = 5;
    public int BatchSize { get; init; } = 100;
    public double MaxDeltaDegrees { get; init; } = 0.0007;
}
