namespace DomoNow.Parking.Application.Authentication;

public interface IOperatorCredentialStore
{
    /// <returns>El operador si las credenciales son válidas; si no, null.</returns>
    Task<AuthenticatedOperator?> ValidateAsync(string username, string password, CancellationToken cancellationToken);
}
