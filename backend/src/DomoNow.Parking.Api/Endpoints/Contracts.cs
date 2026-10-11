namespace DomoNow.Parking.Api.Endpoints;

public sealed record LoginRequest(string? Username, string? Password);

public sealed record CheckInRequest(Guid ParkingSpotId, string? LicensePlate, string? VisitorName, string? DestinationUnit);

public sealed record CheckOutRequest(DateTimeOffset? CheckedOutAt);
