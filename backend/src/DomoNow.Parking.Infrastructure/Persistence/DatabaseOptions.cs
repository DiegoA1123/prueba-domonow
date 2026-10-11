namespace DomoNow.Parking.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool MigrateAndSeedOnStartup { get; init; }
}
