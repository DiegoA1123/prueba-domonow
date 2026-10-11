using System.Text.RegularExpressions;
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public sealed partial record SpotCode
{
    private SpotCode(string value) => Value = value;

    public string Value { get; }

    public static Result<SpotCode> Create(string? raw)
    {
        var normalized = raw?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!ValidFormat().IsMatch(normalized))
        {
            return ParkingErrors.InvalidSpotCode(raw);
        }

        return new SpotCode(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^P[0-9]{2}$")]
    private static partial Regex ValidFormat();
}
