using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderTracking.Application.Abstractions.Caching;
using OrderTracking.Application.Abstractions.Messaging;
using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Infrastructure.Caching;
using OrderTracking.Infrastructure.Messaging;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Infrastructure.Persistence;
using OrderTracking.Infrastructure.Persistence.Repositories;
using OrderTracking.Infrastructure.Realtime;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace OrderTracking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrderTracking")
            ?? throw new InvalidOperationException("Connection string 'OrderTracking' is not configured.");

        services.AddDbContext<OrderTrackingDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.UseNetTopologySuite()));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrderTrackingDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IDriverAssignmentRepository, DriverAssignmentRepository>();
        services.AddRealtimeFallback();
        services.AddRedisCaching(configuration);
        services.AddRabbitMqMessaging(configuration);
        return services;
    }

    private static IServiceCollection AddRealtimeFallback(this IServiceCollection services)
    {
        services.AddSingleton<ITrackingNotifier, NoOpTrackingNotifier>();
        return services;
    }

    private static IServiceCollection AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var options = GetRedisOptions(configuration);
        if (!options.Enabled)
        {
            services.AddSingleton<IActiveOrdersCache, NoOpActiveOrdersCache>();
            return services;
        }

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options.Configuration));
        services.AddSingleton<IActiveOrdersCache, RedisActiveOrdersCache>();
        return services;
    }

    private static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var options = GetRabbitMqOptions(configuration);
        services.Configure<RabbitMqOptions>(configured =>
        {
            configured.Enabled = options.Enabled;
            configured.HostName = options.HostName;
            configured.Port = options.Port;
            configured.UserName = options.UserName;
            configured.Password = options.Password;
            configured.VirtualHost = options.VirtualHost;
            configured.ExchangeName = options.ExchangeName;
        });
        if (!options.Enabled)
        {
            services.AddSingleton<IOrderTrackingEventPublisher, NoOpOrderTrackingEventPublisher>();
            return services;
        }

        services.AddSingleton<IConnection>(_ =>
        {
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                DispatchConsumersAsync = true
            };
            return factory.CreateConnection("order-tracking-api");
        });
        services.AddSingleton<IOrderTrackingEventPublisher, RabbitMqOrderTrackingEventPublisher>();
        services.AddHostedService<RabbitMqAnalyticsConsumer>();
        return services;
    }

    private static RedisOptions GetRedisOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Redis");
        return new RedisOptions
        {
            Enabled = GetBool(section, "Enabled", false),
            Configuration = section["Configuration"] ?? "localhost:6379"
        };
    }

    private static RabbitMqOptions GetRabbitMqOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("RabbitMQ");
        return new RabbitMqOptions
        {
            Enabled = GetBool(section, "Enabled", false),
            HostName = section["HostName"] ?? "localhost",
            Port = GetInt(section, "Port", 5672),
            UserName = section["UserName"] ?? "guest",
            Password = section["Password"] ?? "guest",
            VirtualHost = section["VirtualHost"] ?? "/",
            ExchangeName = section["ExchangeName"] ?? "order-tracking.events"
        };
    }

    private static bool GetBool(IConfiguration configuration, string key, bool defaultValue) =>
        bool.TryParse(configuration[key], out var value) ? value : defaultValue;

    private static int GetInt(IConfiguration configuration, string key, int defaultValue) =>
        int.TryParse(configuration[key], out var value) ? value : defaultValue;
}
