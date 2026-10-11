using DomoNow.Parking.Domain.Common;
using DomoNow.Parking.Domain.ParkingSpots;

namespace DomoNow.Parking.Domain.UnitTests;

public sealed class LicensePlateTests
{
    [Theory]
    [InlineData("abc-123", "ABC123")]
    [InlineData(" ABC 123 ", "ABC123")]
    [InlineData("abc.12d", "ABC12D")]
    [InlineData("ABC123", "ABC123")]
    public void Create_WithValidInput_StoresNormalizedValue(string raw, string expected)
    {
        var result = LicensePlate.Create(raw);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenMissing_ReturnsRequiredError(string? raw)
    {
        var result = LicensePlate.Create(raw);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Parking.LicensePlateRequired");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Theory]
    [InlineData("AB-12")]
    [InlineData("ABCD123")]
    [InlineData("123ABC")]
    [InlineData("ABC1234")]
    [InlineData("ÑAB123")]
    [InlineData("ABC12DD")]
    public void Create_WithInvalidFormat_ReturnsInvalidError(string raw)
    {
        var result = LicensePlate.Create(raw);

        result.Error.Code.ShouldBe("Parking.InvalidLicensePlate");
    }

    [Fact]
    public void Create_WithOversizedInput_ReturnsInvalidError()
    {
        var result = LicensePlate.Create(new string('A', 200));

        result.Error.Code.ShouldBe("Parking.InvalidLicensePlate");
    }

    [Fact]
    public void Plates_WithSameNormalizedValue_AreEqual()
    {
        LicensePlate.Create("abc-123").Value.ShouldBe(LicensePlate.Create("ABC 123").Value);
    }
}

public sealed class SpotCodeTests
{
    [Theory]
    [InlineData("P01", "P01")]
    [InlineData("p15", "P15")]
    public void Create_WithValidCode_Succeeds(string raw, string expected) =>
        SpotCode.Create(raw).Value.Value.ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("P1")]
    [InlineData("X01")]
    [InlineData("P001")]
    public void Create_WithInvalidCode_Fails(string? raw) =>
        SpotCode.Create(raw).Error.Code.ShouldBe("Parking.InvalidSpotCode");
}

public sealed class VisitorNameTests
{
    [Fact]
    public void Create_TrimsValue() =>
        VisitorName.Create("  Carlos Pérez ").Value.Value.ShouldBe("Carlos Pérez");

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("A")]
    public void Create_WhenTooShortOrMissing_Fails(string? raw) =>
        VisitorName.Create(raw).Error.Code.ShouldBe("Parking.InvalidVisitorName");

    [Fact]
    public void Create_WhenLongerThan100_Fails() =>
        VisitorName.Create(new string('a', 101)).Error.Code.ShouldBe("Parking.InvalidVisitorName");
}

public sealed class DestinationUnitTests
{
    [Fact]
    public void Create_TrimsValue() =>
        DestinationUnit.Create(" 302 ").Value.Value.ShouldBe("302");

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_WhenMissing_Fails(string? raw) =>
        DestinationUnit.Create(raw).Error.Code.ShouldBe("Parking.InvalidDestinationUnit");

    [Fact]
    public void Create_WhenLongerThan20_Fails() =>
        DestinationUnit.Create(new string('1', 21)).Error.Code.ShouldBe("Parking.InvalidDestinationUnit");
}
