using System.Text;
using DomoNow.Parking.Application.Abstractions;
using DomoNow.Parking.Application.Authentication;
using DomoNow.Parking.Application.Queries;
using DomoNow.Parking.Infrastructure.Identity;
using DomoNow.Parking.Infrastructure.Persistence;
using DomoNow.Parking.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
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
                "Falta la cadena de conexión 'ConnectionStrings:Parking'. En desarrollo se lee de dotnet user-secrets, " +
                "que solo se cargan con ASPNETCORE_ENVIRONMENT=Development (perfil 'http' o 'https'); " +
                "fuera de desarrollo, configúrela con la variable de entorno ConnectionStrings__Parking.");

        services.AddDbContext<ParkingDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IParkingSpotRepository, ParkingSpotRepository>();
        services.AddScoped<IParkingReadModel, ParkingReadModel>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                jwt => Encoding.UTF8.GetByteCount(jwt.SigningKey) >= JwtOptions.MinimumKeyBytes,
                $"Jwt:SigningKey debe tener al menos {JwtOptions.MinimumKeyBytes} bytes (configúrela con dotnet user-secrets).")
            .Validate(jwt => jwt.ExpiresInMinutes > 0, "Jwt:ExpiresInMinutes debe ser positivo.")
            .ValidateOnStart();
        services.Configure<SeedOperatorOptions>(configuration.GetSection(SeedOperatorOptions.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddSingleton<IPasswordHasher<Operator>, PasswordHasher<Operator>>();
        services.AddScoped<IOperatorCredentialStore, OperatorCredentialStore>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        return services;
    }
}
