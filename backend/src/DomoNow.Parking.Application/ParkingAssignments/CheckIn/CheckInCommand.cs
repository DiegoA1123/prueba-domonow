namespace DomoNow.Parking.Application.ParkingAssignments.CheckIn;

public sealed record CheckInCommand(Guid ParkingSpotId, string? LicensePlate, string? VisitorName, string? DestinationUnit);
