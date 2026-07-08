using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderTracking.Application.Abstractions.Caching;
using OrderTracking.Application.Orders;
using StackExchange.Redis;

namespace OrderTracking.Infrastructure.Caching;

internal sealed class RedisActiveOrdersCache(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisActiveOrdersCache> logger)
    : IActiveOrdersCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string VersionKey = "orders:active:version";
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<IReadOnlyList<OrderDto>?> GetAsync(int skip, int take, CancellationToken cancellationToken)
    {
        try
        {
            var version = await GetVersionAsync();
            var value = await _database.StringGetAsync(BuildKey(version, skip, take));
            return value.HasValue
                ? JsonSerializer.Deserialize<IReadOnlyList<OrderDto>>(value!, JsonOptions)
                : null;
        }
        catch (RedisException exception)
        {
            LogRedisReadFailed(logger, exception);
            return null;
        }
    }

    public async Task SetAsync(int skip, int take, IReadOnlyList<OrderDto> orders, CancellationToken cancellationToken)
    {
        try
        {
            var version = await GetVersionAsync();
            var payload = JsonSerializer.Serialize(orders, JsonOptions);
            await _database.StringSetAsync(BuildKey(version, skip, take), payload, TimeSpan.FromSeconds(30));
        }
        catch (RedisException exception)
        {
            LogRedisWriteFailed(logger, exception);
        }
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _database.StringIncrementAsync(VersionKey);
        }
        catch (RedisException exception)
        {
            LogRedisInvalidationFailed(logger, exception);
        }
    }

    private async Task<long> GetVersionAsync()
    {
        var value = await _database.StringGetAsync(VersionKey);
        if (value.HasValue && long.TryParse(value!, out var version))
            return version;

        await _database.StringSetAsync(VersionKey, 1);
        return 1;
    }

    private static string BuildKey(long version, int skip, int take) => $"orders:active:v{version}:{skip}:{take}";

    private static readonly Action<ILogger, Exception?> LogRedisReadFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3100, nameof(LogRedisReadFailed)),
            "Redis read failed for active orders cache.");

    private static readonly Action<ILogger, Exception?> LogRedisWriteFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3101, nameof(LogRedisWriteFailed)),
            "Redis write failed for active orders cache.");

    private static readonly Action<ILogger, Exception?> LogRedisInvalidationFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3102, nameof(LogRedisInvalidationFailed)),
            "Redis invalidation failed for active orders cache.");
}
