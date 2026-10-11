using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Application.Queries;
using DomoNow.Parking.Infrastructure.Persistence;
using DomoNow.Parking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DomoNow.Parking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Parking")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:Parking' (configúrela con dotnet user-secrets).");

        services.AddDbContext<ParkingDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IParkingSpotRepository, ParkingSpotRepository>();
        services.AddScoped<IParkingReadModel, ParkingReadModel>();
        return services;
    }
}
