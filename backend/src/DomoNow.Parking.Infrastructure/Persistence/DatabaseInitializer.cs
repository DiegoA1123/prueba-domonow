using DomoNow.Parking.Application.Authentication;
using DomoNow.Parking.Domain.ParkingSpots;
using DomoNow.Parking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DomoNow.Parking.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private const int SpotCount = 30;
    private static readonly string[] OutOfServiceCodes = ["P04", "P17"];

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedSpotsAsync(dbContext, cancellationToken);
        await SeedOperatorAsync(
            dbContext,
            scope.ServiceProvider.GetRequiredService<IOptions<SeedOperatorOptions>>().Value,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher<Operator>>(),
            cancellationToken);
    }

    private static async Task SeedSpotsAsync(ParkingDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.ParkingSpots.AnyAsync(cancellationToken))
        {
            return;
        }

        for (var number = 1; number <= SpotCount; number++)
        {
            var spot = ParkingSpot.Create(SpotCode.Create($"P{number:00}").Value);
            if (OutOfServiceCodes.Contains(spot.Code.Value))
            {
                spot.MarkOutOfService();
            }

            dbContext.ParkingSpots.Add(spot);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedOperatorAsync(
        ParkingDbContext dbContext,
        SeedOperatorOptions seed,
        IPasswordHasher<Operator> hasher,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(seed.Username) || string.IsNullOrEmpty(seed.Password))
        {
            return;
        }

        var username = seed.Username.Trim().ToLowerInvariant();
        if (await dbContext.Operators.AnyAsync(op => op.Username == username, cancellationToken))
        {
            return;
        }

        var newOperator = new Operator { Username = username, Role = Roles.Operator };
        newOperator.PasswordHash = hasher.HashPassword(newOperator, seed.Password);
        dbContext.Operators.Add(newOperator);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
