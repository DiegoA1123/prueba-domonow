using System.Text.RegularExpressions;
using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public sealed partial record LicensePlate
{
    private const int MaxRawLength = 15;

    private LicensePlate(string value) => Value = value;

    public string Value { get; }

    public static Result<LicensePlate> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ParkingErrors.LicensePlateRequired;
        }

        if (raw.Length > MaxRawLength)
        {
            return ParkingErrors.InvalidLicensePlate(raw);
        }

        var normalized = Normalize(raw);
        if (!ValidFormat().IsMatch(normalized))
        {
            return ParkingErrors.InvalidLicensePlate(raw);
        }

        return new LicensePlate(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string raw) =>
        string.Concat(raw.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    // Carro: ABC123 · Moto: ABC12D
    [GeneratedRegex("^[A-Z]{3}([0-9]{3}|[0-9]{2}[A-Z])$")]
    private static partial Regex ValidFormat();
}
