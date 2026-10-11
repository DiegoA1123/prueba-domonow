using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

/// <summary>Entidad interna del agregado <see cref="ParkingSpot"/>: solo se modifica a través de la raíz.</summary>
public sealed class ParkingAssignment : Entity
{
    // EF Core materializa usando este constructor (los nombres de parámetros coinciden con las propiedades).
    private ParkingAssignment(
        Guid id,
        Guid parkingSpotId,
        LicensePlate licensePlate,
        VisitorName visitorName,
        DestinationUnit destinationUnit,
        DateTimeOffset checkedInAt,
        DateTimeOffset? checkedOutAt,
        AssignmentStatus status)
        : base(id)
    {
        ParkingSpotId = parkingSpotId;
        LicensePlate = licensePlate;
        VisitorName = visitorName;
        DestinationUnit = destinationUnit;
        CheckedInAt = checkedInAt;
        CheckedOutAt = checkedOutAt;
        Status = status;
    }

    public Guid ParkingSpotId { get; private set; }
    public LicensePlate LicensePlate { get; private set; }
    public VisitorName VisitorName { get; private set; }
    public DestinationUnit DestinationUnit { get; private set; }
    public DateTimeOffset CheckedInAt { get; private set; }
    public DateTimeOffset? CheckedOutAt { get; private set; }
    public AssignmentStatus Status { get; private set; }

    internal static ParkingAssignment Start(
        Guid parkingSpotId,
        LicensePlate licensePlate,
        VisitorName visitorName,
        DestinationUnit destinationUnit,
        DateTimeOffset checkedInAt) =>
        new(Guid.CreateVersion7(), parkingSpotId, licensePlate, visitorName, destinationUnit,
            checkedInAt, checkedOutAt: null, AssignmentStatus.Active);

    internal Result Finish(DateTimeOffset checkedOutAt, DateTimeOffset now)
    {
        if (Status != AssignmentStatus.Active)
        {
            return ParkingErrors.AssignmentNotActive(Id);
        }

        if (checkedOutAt < CheckedInAt)
        {
            return ParkingErrors.CheckOutBeforeCheckIn;
        }

        if (checkedOutAt > now)
        {
            return ParkingErrors.CheckOutInFuture;
        }

        // Se normaliza a UTC: el cliente puede enviar su zona horaria (p. ej. -05:00) y las fechas se guardan en UTC.
        CheckedOutAt = checkedOutAt.ToUniversalTime();
        Status = AssignmentStatus.Finished;
        return Result.Success();
    }
}
