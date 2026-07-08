using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderTracking.Domain.Assignments;
using OrderTracking.Domain.Drivers;
using OrderTracking.Domain.Orders;

namespace OrderTracking.Infrastructure.Persistence.Configurations;

internal sealed class DriverAssignmentConfiguration : IEntityTypeConfiguration<DriverAssignment>
{
    public void Configure(EntityTypeBuilder<DriverAssignment> builder)
    {
        builder.ToTable("DriverAssignments");
        builder.HasKey(assignment => assignment.Id);
        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(assignment => assignment.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Driver>()
            .WithMany()
            .HasForeignKey(assignment => assignment.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(assignment => assignment.OrderId);
        builder.HasIndex(assignment => new { assignment.DriverId, assignment.CompletedAt });
    }
}
