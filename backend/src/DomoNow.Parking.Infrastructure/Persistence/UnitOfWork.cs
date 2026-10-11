using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DomoNow.Parking.Infrastructure.Persistence;

public sealed class UnitOfWork(ParkingDbContext dbContext) : IUnitOfWork
{
    public async Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra transacción modificó el mismo parqueadero (xmin cambió): perdimos la carrera (R6).
            return ParkingErrors.SpotAlreadyTaken;
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception, ParkingAssignmentConfiguration.ActiveSpotIndex))
        {
            return ParkingErrors.SpotAlreadyTaken;
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception, ParkingAssignmentConfiguration.ActivePlateIndex))
        {
            return ParkingErrors.VehicleAlreadyInsideConflict;
        }
    }

    // Solo se capturan las violaciones conocidas; cualquier otra sigue siendo un error inesperado (500).
    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && postgres.ConstraintName == constraintName;
}
