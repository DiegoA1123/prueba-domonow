using DomoNow.Parking.Domain.ParkingSpots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomoNow.Parking.Infrastructure.Persistence.Configurations;

internal sealed class ParkingSpotConfiguration : IEntityTypeConfiguration<ParkingSpot>
{
    public const string AssignmentsField = "_assignments";

    public void Configure(EntityTypeBuilder<ParkingSpot> builder)
    {
        builder.ToTable("parking_spots");
        builder.HasKey(spot => spot.Id);

        builder.Property(spot => spot.Code)
            .HasConversion(code => code.Value, value => SpotCode.Create(value).Value)
            .HasMaxLength(3)
            .IsRequired();
        builder.HasIndex(spot => spot.Code).IsUnique();

        builder.Property(spot => spot.Status).HasConversion<string>().HasMaxLength(20);

        // Versión del agregado: Npgsql mapea un uint con IsRowVersion a la columna de sistema xmin (ADR-0002).
        builder.Property<uint>("Version").IsRowVersion();

        builder.Ignore(spot => spot.ActiveAssignment);

        builder.HasMany<ParkingAssignment>(AssignmentsField)
            .WithOne()
            .HasForeignKey(assignment => assignment.ParkingSpotId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(AssignmentsField).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
