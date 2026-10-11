using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Persistence;
using DomoNow.Parking.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DomoNow.Parking.Infrastructure.Repositories;

public sealed class ParkingSpotRepository(ParkingDbContext dbContext) : IParkingSpotRepository
{
    public async Task<ParkingSpot?> GetByIdAsync(Guid spotId, CancellationToken cancellationToken)
    {
        var spot = await dbContext.ParkingSpots.SingleOrDefaultAsync(s => s.Id == spotId, cancellationToken);
        if (spot is null)
        {
            return null;
        }

        // El agregado solo carga su asignación activa; el historial no participa en invariantes.
        await dbContext.Entry(spot)
            .Collection<ParkingAssignment>(ParkingSpotConfiguration.AssignmentsField)
            .Query()
            .Where(assignment => assignment.Status == AssignmentStatus.Active)
            .LoadAsync(cancellationToken);

        return spot;
    }

    public async Task<ParkingSpot?> GetByActiveAssignmentIdAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        var spotId = await dbContext.ParkingAssignments
            .Where(a => a.Id == assignmentId && a.Status == AssignmentStatus.Active)
            .Select(a => (Guid?)a.ParkingSpotId)
            .SingleOrDefaultAsync(cancellationToken);

        return spotId is null ? null : await GetByIdAsync(spotId.Value, cancellationToken);
    }

    public Task<bool> IsVehicleInsideAsync(LicensePlate plate, CancellationToken cancellationToken) =>
        dbContext.ParkingAssignments.AnyAsync(
            a => a.LicensePlate == plate && a.Status == AssignmentStatus.Active, cancellationToken);
}
