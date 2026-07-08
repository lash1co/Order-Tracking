namespace OrderTracking.Infrastructure.Caching;

internal sealed class RedisOptions
{
    public bool Enabled { get; set; }
    public string Configuration { get; set; } = "localhost:6379";
}
