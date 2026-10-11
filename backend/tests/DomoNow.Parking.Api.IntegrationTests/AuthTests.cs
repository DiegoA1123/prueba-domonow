using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DomoNow.Parking.Api.IntegrationTests;

[Collection("api")]
public sealed class AuthTests(ParkingApiFactory factory)
{
    [Fact]
    public async Task GetSpots_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/parking-spots");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSpots_WithForgedToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.e30.forged");

        var response = await client.GetAsync("/api/parking-spots");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401WithGenericMessage()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { username = ParkingApiFactory.OperatorUsername, password = "wrong-password" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ProblemCodeAsync()).ShouldBe("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Login_WithValidCredentials_AllowsReadingSpots()
    {
        var client = await factory.CreateOperatorClientAsync();

        var response = await client.GetAsync("/api/parking-spots");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
