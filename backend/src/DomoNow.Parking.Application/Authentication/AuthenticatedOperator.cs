namespace DomoNow.Parking.Application.Authentication;

public sealed record AuthenticatedOperator(Guid Id, string Username, string Role);
