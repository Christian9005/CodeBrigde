namespace CodeBridge.Core.Abstractions;

/// <summary>
/// Represents an I2C bus controller for communicating with sensors and peripherals.
/// </summary>
public interface II2cController : IDisposable
{
    /// <summary>
    /// Scans the I2C bus and returns a list of detected device addresses.
    /// </summary>
    Task<IReadOnlyList<byte>> ScanAsync(CancellationToken ct = default);

    /// <summary>
    /// Writes data to a device at the specified address.
    /// </summary>
    Task WriteAsync(byte address, byte[] data, CancellationToken ct = default);

    /// <summary>
    /// Reads data from a device at the specified address.
    /// </summary>
    Task<byte[]> ReadAsync(byte address, int length, CancellationToken ct = default);

    /// <summary>
    /// Writes to a specific register of a device.
    /// </summary>
    Task WriteRegisterAsync(byte address, byte register, byte[] data, CancellationToken ct = default);

    /// <summary>
    /// Reads from a specific register of a device.
    /// </summary>
    Task<byte[]> ReadRegisterAsync(byte address, byte register, int length, CancellationToken ct = default);
}
