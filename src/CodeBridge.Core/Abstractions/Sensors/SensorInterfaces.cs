namespace CodeBridge.Core.Abstractions.Sensors;

/// <summary>
/// A sensor that measures temperature.
/// Implemented by: DHT11, DHT22, BME280, BMP280, DS18B20, LM35, MAX6675.
/// </summary>
public interface ITemperatureSensor : ISensor<double>
{
    /// <summary>
    /// Reads the temperature in degrees Celsius.
    /// </summary>
    Task<double> ReadTemperatureCelsiusAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads the temperature in degrees Fahrenheit.
    /// </summary>
    Task<double> ReadTemperatureFahrenheitAsync(CancellationToken ct = default);
}

/// <summary>
/// A sensor that measures relative humidity (0-100%).
/// Implemented by: DHT11, DHT22, BME280.
/// </summary>
public interface IHumiditySensor : ISensor<double>
{
    /// <summary>
    /// Reads the relative humidity percentage (0-100).
    /// </summary>
    Task<double> ReadHumidityAsync(CancellationToken ct = default);
}

/// <summary>
/// A sensor that measures atmospheric pressure.
/// Implemented by: BME280, BMP280.
/// </summary>
public interface IPressureSensor : ISensor<double>
{
    /// <summary>
    /// Reads the atmospheric pressure in hectopascals (hPa).
    /// </summary>
    Task<double> ReadPressureHpaAsync(CancellationToken ct = default);

    /// <summary>
    /// Estimates altitude based on pressure and sea-level reference.
    /// </summary>
    Task<double> ReadAltitudeMetersAsync(double seaLevelHpa = 1013.25, CancellationToken ct = default);
}

/// <summary>
/// A sensor that measures ambient light intensity.
/// Implemented by: BH1750, TSL2561, LDR (analog).
/// </summary>
public interface ILightSensor : ISensor<double>
{
    /// <summary>
    /// Reads the light intensity in lux.
    /// </summary>
    Task<double> ReadLuxAsync(CancellationToken ct = default);
}

/// <summary>
/// A sensor that measures distance to an object.
/// Implemented by: HC-SR04 (ultrasonic), VL53L0X (ToF laser), IR Sharp.
/// </summary>
public interface IDistanceSensor : ISensor<double>
{
    /// <summary>
    /// Reads the distance in centimeters.
    /// </summary>
    Task<double> ReadDistanceCmAsync(CancellationToken ct = default);

    /// <summary>
    /// Minimum measurable distance in cm.
    /// </summary>
    double MinRangeCm { get; }

    /// <summary>
    /// Maximum measurable distance in cm.
    /// </summary>
    double MaxRangeCm { get; }
}

/// <summary>
/// A sensor that detects motion/presence.
/// Implemented by: PIR HC-SR501, RCWL-0516 radar.
/// </summary>
public interface IMotionSensor : ISensor<bool>
{
    /// <summary>
    /// Returns true if motion is currently detected.
    /// </summary>
    Task<bool> IsMotionDetectedAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when motion is detected.
    /// </summary>
    event EventHandler? MotionDetected;

    /// <summary>
    /// Event raised when motion stops.
    /// </summary>
    event EventHandler? MotionEnded;
}

/// <summary>
/// A sensor that measures gas concentration.
/// Implemented by: MQ-2, MQ-135 (analog), CCS811, SGP30 (I2C).
/// </summary>
public interface IGasSensor : ISensor<double>
{
    /// <summary>
    /// Type of gas this sensor detects (e.g., "CO2", "Smoke", "VOC").
    /// </summary>
    string GasType { get; }

    /// <summary>
    /// Reads the gas concentration in parts per million (ppm).
    /// </summary>
    Task<double> ReadPpmAsync(CancellationToken ct = default);
}

/// <summary>
/// Inertial Measurement Unit — accelerometer + gyroscope + optional magnetometer.
/// Implemented by: MPU6050, MPU9250, ADXL345.
/// </summary>
public interface IImuSensor : ISensor<ImuReading>
{
    /// <summary>
    /// Reads the acceleration vector in g-force (m/s²).
    /// </summary>
    Task<Vector3> ReadAccelerationAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads the angular velocity in degrees/second.
    /// </summary>
    Task<Vector3> ReadGyroscopeAsync(CancellationToken ct = default);

    /// <summary>
    /// Whether this IMU has a magnetometer.
    /// </summary>
    bool HasMagnetometer { get; }

    /// <summary>
    /// Reads the magnetic field vector in microtesla (µT).
    /// Only available if HasMagnetometer is true.
    /// </summary>
    Task<Vector3> ReadMagnetometerAsync(CancellationToken ct = default);
}

/// <summary>
/// A sensor that measures color.
/// Implemented by: TCS34725.
/// </summary>
public interface IColorSensor : ISensor<RgbColor>
{
    /// <summary>
    /// Reads the RGB color values.
    /// </summary>
    Task<RgbColor> ReadColorAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads the color temperature in Kelvin.
    /// </summary>
    Task<int> ReadColorTemperatureAsync(CancellationToken ct = default);
}

/// <summary>
/// A sensor that measures current, voltage, and power.
/// Implemented by: INA219, ACS712.
/// </summary>
public interface ICurrentSensor : ISensor<PowerReading>
{
    /// <summary>
    /// Reads the current in milliamps (mA).
    /// </summary>
    Task<double> ReadCurrentMaAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads the voltage in volts (V).
    /// </summary>
    Task<double> ReadVoltageAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads the power in milliwatts (mW).
    /// </summary>
    Task<double> ReadPowerMwAsync(CancellationToken ct = default);
}
