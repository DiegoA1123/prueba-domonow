namespace DomoNow.Parking.Application.ParkingAssignments.CheckIn;

public sealed record CheckInResponse(
    Guid AssignmentId,
    Guid ParkingSpotId,
    string SpotCode,
    string LicensePlate,
    string VisitorName,
    string DestinationUnit,
    DateTimeOffset CheckedInAt);
