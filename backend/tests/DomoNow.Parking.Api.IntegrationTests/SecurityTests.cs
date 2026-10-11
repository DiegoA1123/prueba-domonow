using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DomoNow.Parking.Api.IntegrationTests;

[Collection("api")]
public sealed class SecurityTests(ParkingApiFactory factory)
{
    private const string WrongKey = "another-signing-key-that-the-api-does-not-trust!!";
    private const string ClientIpHeader = "X-Test-Client-Ip";

    private async Task<HttpStatusCode> CheckInWithTokenAsync(string? token)
    {
        var client = factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.CheckInAsync(Guid.NewGuid(), "ZZZ999");
        return response.StatusCode;
    }

    [Fact]
    public async Task CheckIn_WithoutToken_Returns401() =>
        (await CheckInWithTokenAsync(null)).ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task CheckIn_WithTokenSignedByAnotherKey_Returns401() =>
        (await CheckInWithTokenAsync(TestTokens.Create(signingKey: WrongKey))).ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task CheckIn_WithExpiredToken_Returns401() =>
        (await CheckInWithTokenAsync(TestTokens.Create(expiresAtUtc: DateTime.UtcNow.AddMinutes(-5))))
            .ShouldBe(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task CheckIn_WithTokenWithoutOperatorRole_Returns403() =>
        (await CheckInWithTokenAsync(TestTokens.Create(role: null))).ShouldBe(HttpStatusCode.Forbidden);

    [Fact]
    public async Task LoginRateLimit_IsPerClientIp_SoOneClientCannotLockOutOthers()
    {
        // Host aparte (limitador nuevo) con un middleware de test que simula la IP del cliente.
        using var ipAwareFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IStartupFilter, FakeClientIpStartupFilter>()));
        var client = ipAwareFactory.CreateClient();

        for (var attempt = 0; attempt < 10; attempt++)
        {
            await LoginFromAsync(client, "10.0.0.1");
        }

        (await LoginFromAsync(client, "10.0.0.1")).ShouldBe(HttpStatusCode.TooManyRequests);
        (await LoginFromAsync(client, "10.0.0.2")).ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<HttpStatusCode> LoginFromAsync(HttpClient client, string ipAddress)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { username = "intruder", password = "wrong" }),
        };
        request.Headers.Add(ClientIpHeader, ipAddress);
        var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private sealed class FakeClientIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(ClientIpHeader, out var ip))
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
                }

                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
