using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>Confirma la transacción. Los conflictos de concurrencia/unicidad vuelven como errores de negocio.</summary>
    Task<Result> CommitAsync(CancellationToken cancellationToken);
}
