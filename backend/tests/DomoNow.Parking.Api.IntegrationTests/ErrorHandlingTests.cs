using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DomoNow.Parking.Api.IntegrationTests;

[Collection("api")]
public sealed class ErrorHandlingTests(ParkingApiFactory factory)
{
    [Fact]
    public async Task MalformedJson_WhenBadRequestsThrow_Returns400NotServerError()
    {
        // En Development, ASP.NET lanza BadHttpRequestException ante JSON inválido (ThrowOnBadRequest = true);
        // el manejador global debe responder 400 y no 500.
        using var throwingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true)));
        var client = throwingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await factory.GetOperatorTokenAsync());
        using var body = new StringContent("{\"parkingSpotId\": ", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/parking-assignments", body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
