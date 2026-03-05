namespace CodeBridge.Core.Abstractions;

/// <summary>
/// OneWire bus controller for single-wire communication.
/// Used by: DS18B20 temperature sensor, iButton, DHT (hybrid).
/// </summary>
public interface IOneWireController : IDisposable
{
    /// <summary>
    /// Scans the OneWire bus and returns addresses of all connected devices.
    /// Each address is a 64-bit identifier (8 bytes in hex).
    /// </summary>
    /// <param name="pin">GPIO pin connected to the OneWire data line.</param>
    Task<string[]> ScanAsync(int pin, CancellationToken ct = default);

    /// <summary>
    /// Reads data from a specific device on the OneWire bus.
    /// </summary>
    /// <param name="pin">GPIO pin for the bus.</param>
    /// <param name="address">64-bit device address (hex string).</param>
    /// <param name="length">Number of bytes to read.</param>
    Task<byte[]> ReadAsync(int pin, string address, int length, CancellationToken ct = default);

    /// <summary>
    /// Writes data to a specific device on the OneWire bus.
    /// </summary>
    Task WriteAsync(int pin, string address, byte[] data, CancellationToken ct = default);

    /// <summary>
    /// Reads temperature from a DS18B20 (convenience method).
    /// Sends the Convert T + Read Scratchpad sequence.
    /// </summary>
    /// <param name="pin">GPIO pin for the bus.</param>
    /// <param name="address">Device address. Use null/empty for single-device bus.</param>
    Task<double> ReadTemperatureAsync(int pin, string? address = null, CancellationToken ct = default);
}
