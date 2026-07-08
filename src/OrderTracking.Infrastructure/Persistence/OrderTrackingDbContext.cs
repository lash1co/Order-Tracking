using Microsoft.EntityFrameworkCore;
using OrderTracking.Application.Abstractions.Persistence;
using OrderTracking.Domain.Assignments;
using OrderTracking.Domain.Drivers;
using OrderTracking.Domain.Orders;
using NetTopologySuite.Geometries;

namespace OrderTracking.Infrastructure.Persistence;

public sealed class OrderTrackingDbContext(DbContextOptions<OrderTrackingDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DriverAssignment> DriverAssignments => Set<DriverAssignment>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SynchronizeDriverLocations();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderTrackingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private void SynchronizeDriverLocations()
    {
        foreach (var entry in ChangeTracker.Entries<Driver>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            var location = entry.Entity.CurrentLocation;
            entry.Property<Point>("Location").CurrentValue = new Point(location.Longitude, location.Latitude)
            {
                SRID = 4326
            };
        }
    }
}
