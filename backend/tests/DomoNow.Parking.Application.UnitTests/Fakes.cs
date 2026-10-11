using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.UnitTests;

internal sealed class FakeParkingSpotRepository : IParkingSpotRepository
{
    public List<ParkingSpot> Spots { get; } = [];
    public HashSet<string> PlatesInside { get; } = [];

    public Task<ParkingSpot?> GetByIdAsync(Guid spotId, CancellationToken cancellationToken) =>
        Task.FromResult(Spots.SingleOrDefault(spot => spot.Id == spotId));

    public Task<ParkingSpot?> GetByActiveAssignmentIdAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        Task.FromResult(Spots.SingleOrDefault(spot => spot.ActiveAssignment?.Id == assignmentId));

    public Task<bool> IsVehicleInsideAsync(LicensePlate plate, CancellationToken cancellationToken) =>
        Task.FromResult(PlatesInside.Contains(plate.Value));
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }
    public Error? FailWith { get; set; }

    public Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        if (FailWith is not null)
        {
            return Task.FromResult(Result.Failure(FailWith));
        }

        Commits++;
        return Task.FromResult(Result.Success());
    }
}
