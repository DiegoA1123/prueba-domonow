using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.ParkingAssignments.CheckIn;

public sealed class CheckInHandler(IParkingSpotRepository spots, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<Result<CheckInResponse>> HandleAsync(CheckInCommand command, CancellationToken cancellationToken)
    {
        var plate = LicensePlate.Create(command.LicensePlate);
        if (plate.IsFailure)
        {
            return plate.Error;
        }

        var visitor = VisitorName.Create(command.VisitorName);
        if (visitor.IsFailure)
        {
            return visitor.Error;
        }

        var unit = DestinationUnit.Create(command.DestinationUnit);
        if (unit.IsFailure)
        {
            return unit.Error;
        }

        var spot = await spots.GetByIdAsync(command.ParkingSpotId, cancellationToken);
        if (spot is null)
        {
            return ParkingErrors.SpotNotFound(command.ParkingSpotId);
        }

        if (await spots.IsVehicleInsideAsync(plate.Value, cancellationToken))
        {
            return ParkingErrors.VehicleAlreadyInside(plate.Value);
        }

        var assignment = spot.CheckIn(plate.Value, visitor.Value, unit.Value, clock.GetUtcNow());
        if (assignment.IsFailure)
        {
            return assignment.Error;
        }

        var committed = await unitOfWork.CommitAsync(cancellationToken);
        if (committed.IsFailure)
        {
            return committed.Error;
        }

        var created = assignment.Value;
        return new CheckInResponse(
            created.Id, spot.Id, spot.Code.Value, created.LicensePlate.Value,
            created.VisitorName.Value, created.DestinationUnit.Value, created.CheckedInAt);
    }
}
