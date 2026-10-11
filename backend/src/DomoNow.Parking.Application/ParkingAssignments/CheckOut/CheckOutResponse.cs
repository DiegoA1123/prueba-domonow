namespace DomoNow.Parking.Application.ParkingAssignments.CheckOut;

public sealed record CheckOutResponse(
    Guid AssignmentId,
    Guid ParkingSpotId,
    string LicensePlate,
    DateTimeOffset CheckedInAt,
    DateTimeOffset CheckedOutAt);
