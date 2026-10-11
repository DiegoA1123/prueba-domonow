using DomoNow.Parking.Api.ErrorHandling;
using DomoNow.Parking.Application.Authentication;

namespace DomoNow.Parking.Api.Endpoints;

internal static class AuthEndpoints
{
    public const string LoginRateLimitPolicy = "login";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, LoginHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new LoginCommand(request.Username, request.Password), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimitPolicy)
            .WithTags("Auth");

        return app;
    }
}
