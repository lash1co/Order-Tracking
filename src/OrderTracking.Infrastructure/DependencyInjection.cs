using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Infrastructure.Persistence;
using OrderTracking.Infrastructure.Persistence.Repositories;

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
        return services;
    }
}
