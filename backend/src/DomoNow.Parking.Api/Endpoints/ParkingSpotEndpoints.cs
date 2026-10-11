using DomoNow.Parking.Application.Queries;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Api.Endpoints;

internal static class ParkingSpotEndpoints
{
    public static IEndpointRouteBuilder MapParkingSpotEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/parking-spots", async (ParkingSpotStatus? status, IParkingReadModel readModel, CancellationToken ct) =>
                Results.Ok(await readModel.GetSpotsAsync(status, ct)))
            .RequireAuthorization()
            .WithTags("Parking spots");

        return app;
    }
}
