using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public sealed record DestinationUnit
{
    private const int MaxLength = 20;

    private DestinationUnit(string value) => Value = value;

    public string Value { get; }

    public static Result<DestinationUnit> Create(string? raw)
    {
        var trimmed = raw?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > MaxLength)
        {
            return ParkingErrors.InvalidDestinationUnit;
        }

        return new DestinationUnit(trimmed);
    }

    public override string ToString() => Value;
}
