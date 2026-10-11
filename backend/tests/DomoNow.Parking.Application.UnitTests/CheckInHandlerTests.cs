using DomoNow.Parking.Application.ParkingAssignments.CheckIn;
using DomoNow.Parking.Domain.ParkingSpots;
using Microsoft.Extensions.Time.Testing;

namespace DomoNow.Parking.Application.UnitTests;

public sealed class CheckInHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeParkingSpotRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CheckInHandler _handler;
    private readonly ParkingSpot _spot = ParkingSpot.Create(SpotCode.Create("P15").Value);

    public CheckInHandlerTests()
    {
        _repository.Spots.Add(_spot);
        _handler = new CheckInHandler(_repository, _unitOfWork, new FakeTimeProvider(Now));
    }

    private CheckInCommand ValidCommand(string plate = "abc-123") =>
        new(_spot.Id, plate, "Carlos Pérez", "302");

    [Fact]
    public async Task HandleAsync_WithValidCommand_ChecksInAndCommits()
    {
        var result = await _handler.HandleAsync(ValidCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.LicensePlate.ShouldBe("ABC123");
        result.Value.SpotCode.ShouldBe("P15");
        result.Value.CheckedInAt.ShouldBe(Now);
        _spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
        _unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPlate_ReturnsValidationErrorWithoutCommit()
    {
        var result = await _handler.HandleAsync(ValidCommand("12"), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.InvalidLicensePlate");
        _unitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WhenSpotDoesNotExist_ReturnsNotFound()
    {
        var command = ValidCommand() with { ParkingSpotId = Guid.NewGuid() };

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.SpotNotFound");
    }

    [Fact]
    public async Task HandleAsync_WhenVehicleAlreadyInside_ReturnsConflictAndLeavesSpotAvailable()
    {
        _repository.PlatesInside.Add("ABC123");

        var result = await _handler.HandleAsync(ValidCommand(), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.VehicleAlreadyInside");
        _spot.Status.ShouldBe(ParkingSpotStatus.Available);
        _unitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WhenSpotOccupied_ReturnsDomainError()
    {
        await _handler.HandleAsync(ValidCommand("XYZ987"), CancellationToken.None);

        var result = await _handler.HandleAsync(ValidCommand(), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.SpotOccupied");
    }

    [Fact]
    public async Task HandleAsync_WhenCommitConflicts_PropagatesConflict()
    {
        _unitOfWork.FailWith = ParkingErrors.SpotAlreadyTaken;

        var result = await _handler.HandleAsync(ValidCommand(), CancellationToken.None);

        result.Error.ShouldBe(ParkingErrors.SpotAlreadyTaken);
    }
}
