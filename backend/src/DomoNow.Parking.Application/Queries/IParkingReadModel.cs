using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Application.Queries;

/// <summary>
/// Puerto de lectura. Las consultas no pasan por el agregado: proyectan directo a DTO.
/// No hay handler porque no hay lógica que orquestar (ADR-0003).
/// </summary>
public interface IParkingReadModel
{
    Task<IReadOnlyList<ParkingSpotDto>> GetSpotsAsync(ParkingSpotStatus? status, CancellationToken cancellationToken);

    Task<IReadOnlyList<ActiveAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken);
}
