namespace DomoNow.Parking.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; init; } = "domonow-parking-api";
    public string Audience { get; init; } = "domonow-parking-web";
    public string SigningKey { get; init; } = string.Empty;
    public int ExpiresInMinutes { get; init; } = 60;
}
