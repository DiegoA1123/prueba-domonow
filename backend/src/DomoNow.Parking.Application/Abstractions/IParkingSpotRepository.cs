using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.Abstractions;

/// <summary>Puerto de salida: un repositorio por agregado. Devuelve la raíz con solo su asignación activa.</summary>
public interface IParkingSpotRepository
{
    Task<ParkingSpot?> GetByIdAsync(Guid spotId, CancellationToken cancellationToken);

    Task<ParkingSpot?> GetByActiveAssignmentIdAsync(Guid assignmentId, CancellationToken cancellationToken);

    /// <summary>R7: consulta de conjunto que cruza agregados (ADR-0005).</summary>
    Task<bool> IsVehicleInsideAsync(LicensePlate plate, CancellationToken cancellationToken);
}
