using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.Queries;

public sealed record ParkingSpotDto(
    Guid Id,
    string Code,
    ParkingSpotStatus Status,
    bool IsAvailable,
    ActiveAssignmentDto? ActiveAssignment);
