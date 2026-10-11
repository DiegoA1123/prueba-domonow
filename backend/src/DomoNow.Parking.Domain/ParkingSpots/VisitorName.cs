using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public sealed record VisitorName
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    private VisitorName(string value) => Value = value;

    public string Value { get; }

    public static Result<VisitorName> Create(string? raw)
    {
        var trimmed = raw?.Trim() ?? string.Empty;
        if (trimmed.Length is < MinLength or > MaxLength)
        {
            return ParkingErrors.InvalidVisitorName;
        }

        return new VisitorName(trimmed);
    }

    public override string ToString() => Value;
}
