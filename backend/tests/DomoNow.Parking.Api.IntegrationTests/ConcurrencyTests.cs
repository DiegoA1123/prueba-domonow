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
    public async Task SimultaneousCheckOutsOfSameAssignment_OnlyOneSucceedsWithAccurateConflict()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P13");
        var created = await (await client.CheckInAsync(spot.Id, "DDD123"))
            .ReadAsync<Application.ParkingAssignments.CheckIn.CheckInResponse>();

        var responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => client.PutAsync($"/api/parking-assignments/{created.AssignmentId}/checkout", null)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        foreach (var loser in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
        {
            loser.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Conflict);
            if (loser.StatusCode == HttpStatusCode.Conflict)
            {
                // Nunca el mensaje de check-in ("el parqueadero acaba de ser asignado") en una salida: o la asignación
                // ya no está activa (cargó tras el commit del ganador) o perdió la carrera de versión.
                (await loser.ProblemCodeAsync()).ShouldBeOneOf("Parking.ConcurrentModification", "Parking.AssignmentNotActive");
            }
        }

        (await client.GetSpotAsync("P13")).Status.ShouldBe(Domain.ParkingSpots.ParkingSpotStatus.Available);
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
