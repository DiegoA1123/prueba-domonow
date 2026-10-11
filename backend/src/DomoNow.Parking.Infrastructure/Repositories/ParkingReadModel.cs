using DomoNow.Parking.Application.Queries;
using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DomoNow.Parking.Infrastructure.Repositories;

public sealed class ParkingReadModel(ParkingDbContext dbContext) : IParkingReadModel
{
    public async Task<IReadOnlyList<ParkingSpotDto>> GetSpotsAsync(
        ParkingSpotStatus? status, CancellationToken cancellationToken)
    {
        var spots = await dbContext.ParkingSpots.AsNoTracking()
            .Where(spot => status == null || spot.Status == status)
            .OrderBy(spot => spot.Code)
            .ToListAsync(cancellationToken);

        var activeBySpot = (await GetActiveAssignmentsAsync(cancellationToken))
            .ToDictionary(assignment => assignment.ParkingSpotId);

        return spots
            .Select(spot => new ParkingSpotDto(
                spot.Id,
                spot.Code.Value,
                spot.Status,
                spot.Status == ParkingSpotStatus.Available,
                activeBySpot.GetValueOrDefault(spot.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<ActiveAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken)
    {
        // Se materializan entidades (sin tracking) y se mapea en memoria: EF no traduce .Value de los VOs convertidos.
        var rows = await (
                from assignment in dbContext.ParkingAssignments.AsNoTracking()
                where assignment.Status == AssignmentStatus.Active
                join spot in dbContext.ParkingSpots.AsNoTracking() on assignment.ParkingSpotId equals spot.Id
                orderby assignment.CheckedInAt
                select new { assignment, spot })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ActiveAssignmentDto(
                row.assignment.Id,
                row.spot.Id,
                row.spot.Code.Value,
                row.assignment.LicensePlate.Value,
                row.assignment.VisitorName.Value,
                row.assignment.DestinationUnit.Value,
                row.assignment.CheckedInAt))
            .ToList();
    }
}
