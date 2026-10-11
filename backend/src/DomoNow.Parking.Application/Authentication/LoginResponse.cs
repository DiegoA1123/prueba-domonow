namespace DomoNow.Parking.Application.Authentication;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string Username, string Role);
