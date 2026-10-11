namespace DomoNow.Parking.Infrastructure.Identity;

public sealed class SeedOperatorOptions
{
    public const string SectionName = "SeedOperator";

    public string? Username { get; init; }
    public string? Password { get; init; }
}
