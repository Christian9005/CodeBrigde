using CodeBridge.Core.Abstractions.Sensors;
using Xunit;

namespace CodeBridge.Core.Tests.Abstractions;

/// <summary>
/// Tests for sensor model types (value types, records, formatting).
/// </summary>
public class SensorModelTests
{
    // ── Vector3 ──────────────────────────────────────────────

    [Fact]
    public void Vector3_Magnitude_Calculated_Correctly()
    {
        var v = new Vector3(3, 4, 0);
        Assert.Equal(5.0, v.Magnitude, precision: 4);
    }

    [Fact]
    public void Vector3_Zero_Has_Zero_Magnitude()
    {
        Assert.Equal(0, Vector3.Zero.Magnitude);
    }

    [Fact]
    public void Vector3_ToString_Formats_Values()
    {
        var v = new Vector3(1.23, -4.56, 7.89);
        Assert.Equal("(1.23, -4.56, 7.89)", v.ToString());
    }

    [Fact]
    public void Vector3_Equality_Works()
    {
        var a = new Vector3(1, 2, 3);
        var b = new Vector3(1, 2, 3);
        Assert.Equal(a, b);
    }

    // ── RgbColor ─────────────────────────────────────────────

    [Fact]
    public void RgbColor_ToHex_Returns_Correct_String()
    {
        var color = new RgbColor(255, 128, 0);
        Assert.Equal("#FF8000", color.ToHex());
    }

    [Fact]
    public void RgbColor_ToString_Shows_RGB()
    {
        var color = new RgbColor(10, 20, 30);
        Assert.Equal("RGB(10, 20, 30)", color.ToString());
    }

    [Fact]
    public void RgbColor_With_Clear_Channel()
    {
        var color = new RgbColor(100, 150, 200, 1024);
        Assert.Equal((ushort)1024, color.Clear);
    }

    // ── ImuReading ───────────────────────────────────────────

    [Fact]
    public void ImuReading_Without_Magnetometer()
    {
        var reading = new ImuReading(
            new Vector3(0, 0, 9.81),
            new Vector3(0, 0, 0));
        Assert.Null(reading.Magnetometer);
    }

    [Fact]
    public void ImuReading_With_Magnetometer()
    {
        var reading = new ImuReading(
            new Vector3(0, 0, 9.81),
            new Vector3(1, 2, 3),
            new Vector3(30, -5, 40));
        Assert.NotNull(reading.Magnetometer);
        Assert.Equal(30, reading.Magnetometer.Value.X);
    }

    // ── PowerReading ─────────────────────────────────────────

    [Fact]
    public void PowerReading_ToString_Formats_Correctly()
    {
        var reading = new PowerReading(150.5, 3.30, 496.65);
        Assert.Contains("150.5mA", reading.ToString());
        Assert.Contains("3.30V", reading.ToString());
    }

    // ── EnvironmentReading ───────────────────────────────────

    [Fact]
    public void EnvironmentReading_Celsius_To_Fahrenheit()
    {
        var reading = new EnvironmentReading(100.0);
        Assert.Equal(212.0, reading.TemperatureFahrenheit, precision: 1);
    }

    [Fact]
    public void EnvironmentReading_Freezing_Point_Conversion()
    {
        var reading = new EnvironmentReading(0.0);
        Assert.Equal(32.0, reading.TemperatureFahrenheit, precision: 1);
    }

    [Fact]
    public void EnvironmentReading_ToString_TempOnly()
    {
        var reading = new EnvironmentReading(22.5);
        Assert.Contains("22.5°C", reading.ToString());
        Assert.DoesNotContain("RH", reading.ToString());
    }

    [Fact]
    public void EnvironmentReading_ToString_Full()
    {
        var reading = new EnvironmentReading(22.5, 65.0, 1013.25);
        var str = reading.ToString();
        Assert.Contains("22.5°C", str);
        Assert.Contains("RH", str);
        Assert.Contains("hPa", str);
    }

    [Fact]
    public void EnvironmentReading_Optional_Fields_Default_Null()
    {
        var reading = new EnvironmentReading(25.0);
        Assert.Null(reading.HumidityPercent);
        Assert.Null(reading.PressureHpa);
    }
}
