using DomoNow.Parking.Application.ParkingAssignments.CheckOut;
using DomoNow.Parking.Domain.ParkingSpots;
using Microsoft.Extensions.Time.Testing;

namespace DomoNow.Parking.Application.UnitTests;

public sealed class CheckOutHandlerTests
{
    private static readonly DateTimeOffset CheckedInAt = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeParkingSpotRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(CheckedInAt);
    private readonly ParkingSpot _spot = ParkingSpot.Create(SpotCode.Create("P02").Value);
    private readonly ParkingAssignment _assignment;
    private readonly CheckOutHandler _handler;

    public CheckOutHandlerTests()
    {
        _assignment = _spot.CheckIn(
            LicensePlate.Create("ABC123").Value,
            VisitorName.Create("Carlos Pérez").Value,
            DestinationUnit.Create("302").Value,
            CheckedInAt).Value;
        _repository.Spots.Add(_spot);
        _clock.Advance(TimeSpan.FromHours(2));
        _handler = new CheckOutHandler(_repository, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_WithoutExplicitTime_UsesServerTime()
    {
        var result = await _handler.HandleAsync(new CheckOutCommand(_assignment.Id, null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CheckedOutAt.ShouldBe(_clock.GetUtcNow());
        _spot.Status.ShouldBe(ParkingSpotStatus.Available);
        _unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_WithExplicitPastTime_UsesIt()
    {
        var lateRegistration = CheckedInAt.AddMinutes(30);

        var result = await _handler.HandleAsync(new CheckOutCommand(_assignment.Id, lateRegistration), CancellationToken.None);

        result.Value.CheckedOutAt.ShouldBe(lateRegistration);
    }

    [Fact]
    public async Task HandleAsync_WithTimeBeforeCheckIn_ReturnsBusinessRuleError()
    {
        var result = await _handler.HandleAsync(
            new CheckOutCommand(_assignment.Id, CheckedInAt.AddMinutes(-5)), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.CheckOutBeforeCheckIn");
        _unitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownAssignment_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(new CheckOutCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.Error.Code.ShouldBe("Parking.ActiveAssignmentNotFound");
    }
}
