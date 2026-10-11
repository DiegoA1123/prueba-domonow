using System.Net;
using System.Net.Http.Json;
using System.Text;
using DomoNow.Parking.Application.ParkingAssignments.CheckIn;
using DomoNow.Parking.Application.Queries;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Api.IntegrationTests;

[Collection("api")]
public sealed class ParkingFlowTests(ParkingApiFactory factory)
{
    [Fact]
    public async Task Spots_AreSeededWithOutOfServiceOnes()
    {
        var client = await factory.CreateOperatorClientAsync();

        var spots = await (await client.GetAsync("/api/parking-spots")).ReadAsync<List<ParkingSpotDto>>();

        spots.Count.ShouldBe(30);
        spots.Single(s => s.Code == "P04").Status.ShouldBe(ParkingSpotStatus.OutOfService);
    }

    [Fact]
    public async Task GetSpots_FilteredByStatus_ReturnsOnlyThatStatus()
    {
        var client = await factory.CreateOperatorClientAsync();

        var spots = await (await client.GetAsync("/api/parking-spots?status=OutOfService"))
            .ReadAsync<List<ParkingSpotDto>>();

        spots.Select(s => s.Code).ShouldBe(["P04", "P17"]);
    }

    [Fact]
    public async Task CheckIn_ThenCheckOut_ReleasesSpot()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P01");

        var checkIn = await client.CheckInAsync(spot.Id, "aaa-111");
        checkIn.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await checkIn.ReadAsync<CheckInResponse>();
        created.LicensePlate.ShouldBe("AAA111");
        (await client.GetSpotAsync("P01")).Status.ShouldBe(ParkingSpotStatus.Occupied);

        var checkOut = await client.PutAsync($"/api/parking-assignments/{created.AssignmentId}/checkout", null);

        checkOut.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetSpotAsync("P01")).Status.ShouldBe(ParkingSpotStatus.Available);
    }

    [Fact]
    public async Task CheckIn_OnOccupiedSpot_Returns409()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P02");
        await client.CheckInAsync(spot.Id, "AAA222");

        var second = await client.CheckInAsync(spot.Id, "AAA333");

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await second.ProblemCodeAsync()).ShouldBe("Parking.SpotOccupied");
    }

    [Fact]
    public async Task CheckIn_OnOutOfServiceSpot_Returns422()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P04");

        var response = await client.CheckInAsync(spot.Id, "AAA666");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.ProblemCodeAsync()).ShouldBe("Parking.SpotOutOfService");
    }

    [Fact]
    public async Task CheckIn_SamePlateOnAnotherSpot_Returns409()
    {
        var client = await factory.CreateOperatorClientAsync();
        await client.CheckInAsync((await client.GetSpotAsync("P03")).Id, "AAA444");

        var response = await client.CheckInAsync((await client.GetSpotAsync("P05")).Id, "aaa 444");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ProblemCodeAsync()).ShouldBe("Parking.VehicleAlreadyInside");
    }

    [Fact]
    public async Task CheckIn_WithInvalidPlate_Returns400()
    {
        var client = await factory.CreateOperatorClientAsync();

        var response = await client.CheckInAsync((await client.GetSpotAsync("P08")).Id, "12-AB");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemCodeAsync()).ShouldBe("Parking.InvalidLicensePlate");
    }

    [Fact]
    public async Task CheckIn_WithMalformedJsonBody_Returns400NotServerError()
    {
        var client = await factory.CreateOperatorClientAsync();
        using var body = new StringContent("{\"parkingSpotId\": ", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/parking-assignments", body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CheckOut_WithTimeBeforeCheckIn_Returns422AndKeepsSpotOccupied()
    {
        var client = await factory.CreateOperatorClientAsync();
        var spot = await client.GetSpotAsync("P06");
        var created = await (await client.CheckInAsync(spot.Id, "AAA777")).ReadAsync<CheckInResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/parking-assignments/{created.AssignmentId}/checkout",
            new { checkedOutAt = created.CheckedInAt.AddHours(-1) });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.ProblemCodeAsync()).ShouldBe("Parking.CheckOutBeforeCheckIn");
        (await client.GetSpotAsync("P06")).Status.ShouldBe(ParkingSpotStatus.Occupied);
    }

    [Fact]
    public async Task CheckOut_UnknownAssignment_Returns404()
    {
        var client = await factory.CreateOperatorClientAsync();

        var response = await client.PutAsync($"/api/parking-assignments/{Guid.NewGuid()}/checkout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActiveAssignments_ListsVehiclesInside()
    {
        var client = await factory.CreateOperatorClientAsync();
        await client.CheckInAsync((await client.GetSpotAsync("P07")).Id, "AAA555");

        var active = await (await client.GetAsync("/api/parking-assignments/active"))
            .ReadAsync<List<ActiveAssignmentDto>>();

        active.ShouldContain(a => a.LicensePlate == "AAA555" && a.SpotCode == "P07");
    }
}
