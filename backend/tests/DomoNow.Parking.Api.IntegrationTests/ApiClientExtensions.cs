using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DomoNow.Parking.Application.Queries;

namespace DomoNow.Parking.Api.IntegrationTests;

internal static class ApiClientExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<HttpClient> CreateOperatorClientAsync(this ParkingApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await factory.GetOperatorTokenAsync());
        return client;
    }

    public static async Task<ParkingSpotDto> GetSpotAsync(this HttpClient client, string code)
    {
        var spots = await client.GetFromJsonAsync<List<ParkingSpotDto>>("/api/parking-spots", Json);
        spots.ShouldNotBeNull();
        return spots.Single(spot => spot.Code == code);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
        where T : class
    {
        var body = await response.Content.ReadFromJsonAsync<T>(Json);
        body.ShouldNotBeNull();
        return body;
    }

    public static Task<HttpResponseMessage> CheckInAsync(this HttpClient client, Guid spotId, string plate) =>
        client.PostAsJsonAsync("/api/parking-assignments",
            new { parkingSpotId = spotId, licensePlate = plate, visitorName = "Visitante Prueba", destinationUnit = "302" });

    public static async Task<string?> ProblemCodeAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
