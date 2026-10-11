using DomoNow.Parking.Api.ErrorHandling;
using DomoNow.Parking.Api.Security;
using DomoNow.Parking.Application.ParkingAssignments.CheckIn;
using DomoNow.Parking.Application.ParkingAssignments.CheckOut;
using DomoNow.Parking.Application.Queries;

namespace DomoNow.Parking.Api.Endpoints;

internal static class ParkingAssignmentEndpoints
{
    public static IEndpointRouteBuilder MapParkingAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parking-assignments").WithTags("Parking assignments");

        group.MapGet("/active", async (IParkingReadModel readModel, CancellationToken ct) =>
                Results.Ok(await readModel.GetActiveAssignmentsAsync(ct)))
            .RequireAuthorization();

        group.MapPost("/", async (CheckInRequest request, CheckInHandler handler, CancellationToken ct) =>
            {
                var command = new CheckInCommand(
                    request.ParkingSpotId, request.LicensePlate, request.VisitorName, request.DestinationUnit);
                var result = await handler.HandleAsync(command, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/parking-assignments/{result.Value.AssignmentId}", result.Value)
                    : result.Error.ToProblem();
            })
            .RequireAuthorization(AuthenticationSetup.OperatorPolicy);

        group.MapPut("/{id:guid}/checkout", async (
                Guid id, CheckOutRequest? request, CheckOutHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new CheckOutCommand(id, request?.CheckedOutAt), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .RequireAuthorization(AuthenticationSetup.OperatorPolicy);

        return app;
    }
}
