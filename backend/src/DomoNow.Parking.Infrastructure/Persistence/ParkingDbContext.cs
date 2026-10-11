using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace DomoNow.Parking.Infrastructure.Persistence;

public sealed class ParkingDbContext(DbContextOptions<ParkingDbContext> options) : DbContext(options)
{
    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
    public DbSet<ParkingAssignment> ParkingAssignments => Set<ParkingAssignment>();
    public DbSet<Operator> Operators => Set<Operator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParkingDbContext).Assembly);
}
