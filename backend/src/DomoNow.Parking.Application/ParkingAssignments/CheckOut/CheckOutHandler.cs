using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.ParkingAssignments.CheckOut;

public sealed class CheckOutHandler(IParkingSpotRepository spots, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<Result<CheckOutResponse>> HandleAsync(CheckOutCommand command, CancellationToken cancellationToken)
    {
        var spot = await spots.GetByActiveAssignmentIdAsync(command.AssignmentId, cancellationToken);
        if (spot is null)
        {
            return ParkingErrors.ActiveAssignmentNotFound(command.AssignmentId);
        }

        var now = clock.GetUtcNow();
        var finished = spot.CheckOut(command.AssignmentId, command.CheckedOutAt ?? now, now);
        if (finished.IsFailure)
        {
            return finished.Error;
        }

        var committed = await unitOfWork.CommitAsync(cancellationToken);
        if (committed.IsFailure)
        {
            return committed.Error;
        }

        var assignment = finished.Value;
        return new CheckOutResponse(
            assignment.Id,
            spot.Id,
            assignment.LicensePlate.Value,
            assignment.CheckedInAt,
            assignment.CheckedOutAt ?? now);
    }
}
