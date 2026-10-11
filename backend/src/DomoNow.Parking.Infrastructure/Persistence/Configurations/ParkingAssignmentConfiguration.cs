using DomoNow.Parking.Domain.ParkingSpots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomoNow.Parking.Infrastructure.Persistence.Configurations;

internal sealed class ParkingAssignmentConfiguration : IEntityTypeConfiguration<ParkingAssignment>
{
    public const string ActiveSpotIndex = "ux_parking_assignments_active_spot";
    public const string ActivePlateIndex = "ux_parking_assignments_active_plate";
    private const string ActiveFilter = "status = 'Active'";

    public void Configure(EntityTypeBuilder<ParkingAssignment> builder)
    {
        builder.ToTable("parking_assignments", table => table.HasCheckConstraint(
            "ck_parking_assignments_checkout_after_checkin",
            "checked_out_at IS NULL OR checked_out_at >= checked_in_at"));
        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.LicensePlate)
            .HasConversion(plate => plate.Value, value => LicensePlate.Create(value).Value)
            .HasMaxLength(6)
            .IsRequired();
        builder.Property(assignment => assignment.VisitorName)
            .HasConversion(name => name.Value, value => VisitorName.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(assignment => assignment.DestinationUnit)
            .HasConversion(unit => unit.Value, value => DestinationUnit.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(assignment => assignment.Status).HasConversion<string>().HasMaxLength(20);

        // R1/R6 y R7: garantías finales en BD bajo concurrencia (ADR-0002, ADR-0005).
        builder.HasIndex(assignment => assignment.ParkingSpotId)
            .IsUnique()
            .HasFilter(ActiveFilter)
            .HasDatabaseName(ActiveSpotIndex);
        builder.HasIndex(assignment => assignment.LicensePlate)
            .IsUnique()
            .HasFilter(ActiveFilter)
            .HasDatabaseName(ActivePlateIndex);
    }
}
