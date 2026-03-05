namespace CodeBridge.Core.Abstractions.Sensors;

/// <summary>
/// 3D vector for IMU readings (acceleration, gyroscope, magnetometer).
/// </summary>
public readonly record struct Vector3(double X, double Y, double Z)
{
    /// <summary>
    /// The magnitude (length) of the vector.
    /// </summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    public static Vector3 Zero => new(0, 0, 0);

    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}

/// <summary>
/// Combined IMU reading with acceleration and gyroscope data.
/// </summary>
public readonly record struct ImuReading(
    Vector3 Acceleration,
    Vector3 Gyroscope,
    Vector3? Magnetometer = null
);

/// <summary>
/// RGB color reading from a color sensor.
/// </summary>
public readonly record struct RgbColor(byte R, byte G, byte B, ushort Clear = 0)
{
    /// <summary>
    /// Returns the color as a hex string (e.g., "#FF8020").
    /// </summary>
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    public override string ToString() => $"RGB({R}, {G}, {B})";
}

/// <summary>
/// Power monitoring reading (current, voltage, power).
/// </summary>
public readonly record struct PowerReading(
    double CurrentMa,
    double VoltageV,
    double PowerMw
)
{
    public override string ToString() => $"{CurrentMa:F1}mA @ {VoltageV:F2}V = {PowerMw:F1}mW";
}

/// <summary>
/// Environment reading combining temperature, humidity, and optional pressure.
/// </summary>
public readonly record struct EnvironmentReading(
    double TemperatureCelsius,
    double? HumidityPercent = null,
    double? PressureHpa = null
)
{
    public double TemperatureFahrenheit => TemperatureCelsius * 9.0 / 5.0 + 32.0;

    public override string ToString()
    {
        var s = $"{TemperatureCelsius:F1}°C";
        if (HumidityPercent.HasValue) s += $", {HumidityPercent:F1}%RH";
        if (PressureHpa.HasValue) s += $", {PressureHpa:F1}hPa";
        return s;
    }
}
