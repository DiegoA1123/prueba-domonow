namespace DomoNow.Parking.Application.Queries;

public sealed record ActiveAssignmentDto(
    Guid AssignmentId,
    Guid ParkingSpotId,
    string SpotCode,
    string LicensePlate,
    string VisitorName,
    string DestinationUnit,
    DateTimeOffset CheckedInAt);
