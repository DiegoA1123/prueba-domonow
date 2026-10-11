using System.Net;
using DomoNow.Parking.Application.Queries;

namespace DomoNow.Parking.Api.IntegrationTests;

[Collection("api")]
public sealed class ConcurrencyTests(ParkingApiFactory factory)
{
    private const int ConcurrentRequests = 20;

    [Fact]
    public async Task SimultaneousCheckInsOnSameSpot_OnlyOneSucceeds()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P10");

        var responses = await Task.WhenAll(Enumerable.Range(0, ConcurrentRequests)
            .Select(i => client.CheckInAsync(spot.Id, $"BBB{100 + i}")));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(ConcurrentRequests - 1);
        var active = await (await client.GetAsync("/api/parking-assignments/active"))
            .ReadAsync<List<ActiveAssignmentDto>>();
        active.Count(a => a.SpotCode == "P10").ShouldBe(1);
    }

    [Fact]
    public async Task SimultaneousCheckInsOfSamePlateOnDifferentSpots_OnlyOneSucceeds()
    {
        var client = await factory.CreateOperatorClientAsync();
        var first = await client.GetSpotAsync("P11");
        var second = await client.GetSpotAsync("P12");

        var responses = await Task.WhenAll(
            client.CheckInAsync(first.Id, "CCC123"),
            client.CheckInAsync(second.Id, "CCC123"));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        var loser = responses.Single(r => r.StatusCode != HttpStatusCode.Created);
        loser.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await loser.ProblemCodeAsync()).ShouldBe("Parking.VehicleAlreadyInside");
    }
}
