using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderTracking.Domain.Drivers;

namespace OrderTracking.Infrastructure.Persistence.Configurations;

internal sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");
        builder.HasKey(driver => driver.Id);
        builder.Property(driver => driver.Name).HasMaxLength(200).IsRequired();
        builder.Property(driver => driver.VehicleType).HasConversion<string>().HasMaxLength(32);
        builder.Property(driver => driver.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(driver => driver.RowVersion).IsRowVersion();
        builder.OwnsOne(driver => driver.CurrentLocation, location =>
        {
            location.Property(value => value.Latitude).HasColumnName("CurrentLat");
            location.Property(value => value.Longitude).HasColumnName("CurrentLong");
        });
        builder.HasIndex(driver => driver.Status);
    }
}
