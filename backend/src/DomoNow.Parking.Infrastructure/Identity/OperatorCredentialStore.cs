using DomoNow.Parking.Application.Authentication;
using DomoNow.Parking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DomoNow.Parking.Infrastructure.Identity;

public sealed class OperatorCredentialStore(ParkingDbContext dbContext, IPasswordHasher<Operator> hasher)
    : IOperatorCredentialStore
{
    // Hash ficticio para que un usuario inexistente cueste lo mismo que uno existente (evita enumeración por tiempo).
    private static readonly Operator TimingDummy = new() { Username = "dummy", Role = Roles.Operator };
    private static readonly string DummyHash = new PasswordHasher<Operator>().HashPassword(TimingDummy, "dummy-password");

    public async Task<AuthenticatedOperator?> ValidateAsync(
        string username, string password, CancellationToken cancellationToken)
    {
        var normalized = username.ToLowerInvariant();
        var found = await dbContext.Operators.AsNoTracking()
            .SingleOrDefaultAsync(op => op.Username == normalized, cancellationToken);

        if (found is null)
        {
            hasher.VerifyHashedPassword(TimingDummy, DummyHash, password);
            return null;
        }

        var verification = hasher.VerifyHashedPassword(found, found.PasswordHash, password);
        return verification == PasswordVerificationResult.Failed
            ? null
            : new AuthenticatedOperator(found.Id, found.Username, found.Role);
    }
}
