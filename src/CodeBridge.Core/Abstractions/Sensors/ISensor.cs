namespace CodeBridge.Core.Abstractions.Sensors;

/// <summary>
/// Base interface for all sensors. Provides a generic async read pattern
/// with metadata about the sensor and continuous streaming support.
/// </summary>
/// <typeparam name="T">The type of reading this sensor produces.</typeparam>
public interface ISensor<T> : IDisposable
{
    /// <summary>
    /// Human-readable sensor name (e.g., "BME280", "DHT22").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The measurement unit (e.g., "°C", "lux", "cm", "ppm").
    /// </summary>
    string Unit { get; }

    /// <summary>
    /// Whether the sensor has been initialized and is ready to read.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Initializes the sensor (configures registers, sets modes, etc.)
    /// </summary>
    Task InitAsync(CancellationToken ct = default);

    /// <summary>
    /// Takes a single reading from the sensor.
    /// </summary>
    Task<T> ReadAsync(CancellationToken ct = default);

    /// <summary>
    /// Streams continuous readings at the specified interval.
    /// </summary>
    /// <param name="interval">Delay between readings.</param>
    /// <param name="ct">Cancellation token to stop streaming.</param>
    IAsyncEnumerable<T> ReadContinuousAsync(
        TimeSpan interval,
        CancellationToken ct = default);
}
