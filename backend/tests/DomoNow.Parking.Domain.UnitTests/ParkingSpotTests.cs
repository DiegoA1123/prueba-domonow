using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Domain.UnitTests;

public sealed class ParkingSpotTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);
    private static readonly LicensePlate Plate = LicensePlate.Create("ABC123").Value;
    private static readonly VisitorName Visitor = VisitorName.Create("Carlos Pérez").Value;
    private static readonly DestinationUnit Unit = DestinationUnit.Create("302").Value;

    private static ParkingSpot AvailableSpot() => ParkingSpot.Create(SpotCode.Create("P15").Value);

    private static (ParkingSpot Spot, ParkingAssignment Assignment) OccupiedSpot()
    {
        var spot = AvailableSpot();
        var assignment = spot.CheckIn(Plate, Visitor, Unit, Now).Value;
        return (spot, assignment);
    }

    [Fact]
    public void Create_StartsAvailableWithoutAssignment()
    {
        var spot = AvailableSpot();

        spot.Status.ShouldBe(ParkingSpotStatus.Available);
        spot.ActiveAssignment.ShouldBeNull();
    }

    [Fact]
    public void CheckIn_WhenAvailable_CreatesActiveAssignmentAndOccupiesSpot()
    {
        var spot = AvailableSpot();

        var result = spot.CheckIn(Plate, Visitor, Unit, Now);

        result.IsSuccess.ShouldBeTrue();
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
        spot.ActiveAssignment.ShouldBe(result.Value);
        result.Value.Status.ShouldBe(AssignmentStatus.Active);
        result.Value.ParkingSpotId.ShouldBe(spot.Id);
        result.Value.LicensePlate.ShouldBe(Plate);
        result.Value.CheckedInAt.ShouldBe(Now);
        result.Value.CheckedOutAt.ShouldBeNull();
    }

    [Fact]
    public void CheckIn_WhenOccupied_ReturnsSpotOccupied()
    {
        var (spot, first) = OccupiedSpot();

        var result = spot.CheckIn(LicensePlate.Create("XYZ987").Value, Visitor, Unit, Now);

        result.Error.Code.ShouldBe("Parking.SpotOccupied");
        spot.ActiveAssignment.ShouldBe(first);
    }

    [Fact]
    public void CheckIn_WhenOutOfService_ReturnsSpotOutOfService()
    {
        var spot = AvailableSpot();
        spot.MarkOutOfService();

        var result = spot.CheckIn(Plate, Visitor, Unit, Now);

        result.Error.Code.ShouldBe("Parking.SpotOutOfService");
        spot.Status.ShouldBe(ParkingSpotStatus.OutOfService);
        spot.ActiveAssignment.ShouldBeNull();
    }

    [Fact]
    public void CheckOut_FinishesAssignmentAndReleasesSpot()
    {
        var (spot, assignment) = OccupiedSpot();
        var exit = Now.AddHours(2);

        var result = spot.CheckOut(assignment.Id, exit, exit);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(AssignmentStatus.Finished);
        result.Value.CheckedOutAt.ShouldBe(exit);
        spot.Status.ShouldBe(ParkingSpotStatus.Available);
        spot.ActiveAssignment.ShouldBeNull();
    }

    [Fact]
    public void CheckOut_AtExactCheckInTime_IsAllowed()
    {
        var (spot, assignment) = OccupiedSpot();

        spot.CheckOut(assignment.Id, Now, Now).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void CheckOut_BeforeCheckIn_ReturnsErrorAndKeepsState()
    {
        var (spot, assignment) = OccupiedSpot();

        var result = spot.CheckOut(assignment.Id, Now.AddMinutes(-1), Now.AddHours(1));

        result.Error.Code.ShouldBe("Parking.CheckOutBeforeCheckIn");
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
        assignment.Status.ShouldBe(AssignmentStatus.Active);
        assignment.CheckedOutAt.ShouldBeNull();
    }

    [Fact]
    public void CheckOut_InTheFuture_ReturnsErrorAndKeepsState()
    {
        var (spot, assignment) = OccupiedSpot();
        var serverNow = Now.AddHours(1);

        var result = spot.CheckOut(assignment.Id, serverNow.AddMinutes(5), serverNow);

        result.Error.Code.ShouldBe("Parking.CheckOutInFuture");
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
    }

    [Fact]
    public void CheckOut_WithUnknownAssignment_ReturnsAssignmentNotActive()
    {
        var (spot, _) = OccupiedSpot();

        var result = spot.CheckOut(Guid.NewGuid(), Now, Now);

        result.Error.Code.ShouldBe("Parking.AssignmentNotActive");
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
    }

    [Fact]
    public void CheckOut_Twice_ReturnsAssignmentNotActive()
    {
        var (spot, assignment) = OccupiedSpot();
        spot.CheckOut(assignment.Id, Now, Now);

        var result = spot.CheckOut(assignment.Id, Now, Now);

        result.Error.Code.ShouldBe("Parking.AssignmentNotActive");
    }

    [Fact]
    public void CheckIn_AfterCheckOut_IsAllowedAgain()
    {
        var (spot, assignment) = OccupiedSpot();
        spot.CheckOut(assignment.Id, Now, Now);

        spot.CheckIn(Plate, Visitor, Unit, Now.AddMinutes(1)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void MarkOutOfService_WhenOccupied_ReturnsSpotOccupied()
    {
        var (spot, _) = OccupiedSpot();

        spot.MarkOutOfService().Error.Code.ShouldBe("Parking.SpotOccupied");
        spot.Status.ShouldBe(ParkingSpotStatus.Occupied);
    }
}
