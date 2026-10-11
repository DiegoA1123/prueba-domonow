using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

/// <summary>
/// Aggregate root. Protege R1 (una asignación activa), R2 (fuera de servicio), R3 (salida libera)
/// y R5 (fechas, vía <see cref="ParkingAssignment"/>). Solo se carga la asignación activa.
/// </summary>
public sealed class ParkingSpot : AggregateRoot
{
    private readonly List<ParkingAssignment> _assignments = [];

    // EF Core materializa usando este constructor.
    private ParkingSpot(Guid id, SpotCode code, ParkingSpotStatus status) : base(id)
    {
        Code = code;
        Status = status;
    }

    public SpotCode Code { get; private set; }
    public ParkingSpotStatus Status { get; private set; }

    public ParkingAssignment? ActiveAssignment =>
        _assignments.SingleOrDefault(assignment => assignment.Status == AssignmentStatus.Active);

    public static ParkingSpot Create(SpotCode code) =>
        new(Guid.CreateVersion7(), code, ParkingSpotStatus.Available);

    public Result<ParkingAssignment> CheckIn(
        LicensePlate licensePlate,
        VisitorName visitorName,
        DestinationUnit destinationUnit,
        DateTimeOffset now)
    {
        if (Status == ParkingSpotStatus.OutOfService)
        {
            return ParkingErrors.SpotOutOfService(Code);
        }

        if (Status == ParkingSpotStatus.Occupied)
        {
            return ParkingErrors.SpotOccupied(Code);
        }

        var assignment = ParkingAssignment.Start(Id, licensePlate, visitorName, destinationUnit, now);
        _assignments.Add(assignment);
        Status = ParkingSpotStatus.Occupied;
        return assignment;
    }

    public Result<ParkingAssignment> CheckOut(Guid assignmentId, DateTimeOffset checkedOutAt, DateTimeOffset now)
    {
        var assignment = ActiveAssignment;
        if (assignment is null || assignment.Id != assignmentId)
        {
            return ParkingErrors.AssignmentNotActive(assignmentId);
        }

        var finished = assignment.Finish(checkedOutAt, now);
        if (finished.IsFailure)
        {
            return finished.Error;
        }

        Status = ParkingSpotStatus.Available;
        return assignment;
    }

    public Result MarkOutOfService()
    {
        if (Status == ParkingSpotStatus.Occupied)
        {
            return ParkingErrors.SpotOccupied(Code);
        }

        Status = ParkingSpotStatus.OutOfService;
        return Result.Success();
    }
}
