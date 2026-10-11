using System.Net.Http.Json;
using DomoNow.Parking.Application.Authentication;
using DomoNow.Parking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DomoNow.Parking.Api.IntegrationTests;

public sealed class ParkingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string OperatorUsername = "portero-test";
    public const string OperatorPassword = "Integration-Tests-Only-1!";

    private static readonly string ConnectionString =
        new ConfigurationBuilder()
            .AddUserSecrets<ParkingApiFactory>()
            .AddEnvironmentVariables()
            .Build()
            .GetConnectionString("ParkingTests")
        ?? throw new InvalidOperationException(
            "Configure 'ConnectionStrings:ParkingTests' (user-secrets o variable de entorno ConnectionStrings__ParkingTests).");

    private Task<string>? _operatorToken;

    // Un solo login por corrida: el endpoint de login tiene rate limiting (10/min) y no debe dispararse en cada test.
    public Task<string> GetOperatorTokenAsync() => _operatorToken ??= LoginAsync();

    private async Task<string> LoginAsync()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { username = OperatorUsername, password = OperatorPassword });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(ApiClientExtensions.Json);
        login.ShouldNotBeNull();
        return login.AccessToken;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Parking", ConnectionString);
        builder.UseSetting("Jwt:SigningKey", TestTokens.SigningKey);
        builder.UseSetting("SeedOperator:Username", OperatorUsername);
        builder.UseSetting("SeedOperator:Password", OperatorPassword);
        builder.UseSetting("Database:MigrateAndSeedOnStartup", "true");
    }

    public async Task InitializeAsync()
    {
        // Base limpia por corrida: se elimina antes de que el host arranque (el arranque migra y siembra).
        var options = new DbContextOptionsBuilder<ParkingDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var dbContext = new ParkingDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        _ = Server; // fuerza el arranque del host → migración + seed
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("api")]
public sealed class ApiTestGroup : ICollectionFixture<ParkingApiFactory>;
