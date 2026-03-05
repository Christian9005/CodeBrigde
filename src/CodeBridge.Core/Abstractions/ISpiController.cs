namespace CodeBridge.Core.Abstractions;

/// <summary>
/// SPI (Serial Peripheral Interface) bus controller.
/// Used by: WS2812 LEDs, RFID RC522, TFT displays, MAX7219, LoRa, SD cards.
/// </summary>
public interface ISpiController : IDisposable
{
    /// <summary>
    /// Performs a full-duplex SPI transfer (write and read simultaneously).
    /// </summary>
    /// <param name="csPin">Chip Select pin for the target device.</param>
    /// <param name="data">Data to send.</param>
    /// <returns>Data received from the device.</returns>
    Task<byte[]> TransferAsync(int csPin, byte[] data, CancellationToken ct = default);

    /// <summary>
    /// Writes data to an SPI device (ignores received data).
    /// </summary>
    Task WriteAsync(int csPin, byte[] data, CancellationToken ct = default);

    /// <summary>
    /// Reads data from an SPI device (sends zeros while reading).
    /// </summary>
    Task<byte[]> ReadAsync(int csPin, int length, CancellationToken ct = default);

    /// <summary>
    /// Sets the SPI clock speed in Hz.
    /// </summary>
    Task SetClockSpeedAsync(int speedHz, CancellationToken ct = default);

    /// <summary>
    /// Sets the SPI mode (0-3), defining clock polarity and phase.
    /// </summary>
    Task SetModeAsync(int mode, CancellationToken ct = default);
}
