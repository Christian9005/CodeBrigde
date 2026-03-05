using CodeBridge.Core.Enums;

namespace CodeBridge.Core.Abstractions;

/// <summary>
/// Represents a microcontroller board.
/// This is the main entry point for interacting with hardware.
/// </summary>
public interface IBoard : IDisposable
{
    /// <summary>
    /// The name/model of the board (e.g., "ESP32 DevKit V1").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The board family (ESP32, Arduino, STM32, PIC).
    /// </summary>
    BoardFamily Family { get; }

    /// <summary>
    /// The firmware version running on the board.
    /// </summary>
    string FirmwareVersion { get; }

    /// <summary>
    /// Whether the board is currently connected and responsive.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// The transport layer used to communicate with the board.
    /// </summary>
    ITransport Transport { get; }

    /// <summary>
    /// GPIO controller for digital/analog pin operations.
    /// </summary>
    IGpioController Gpio { get; }

    /// <summary>
    /// I2C controller for communicating with I2C devices.
    /// </summary>
    II2cController I2C { get; }

    /// <summary>
    /// SPI controller for communicating with SPI devices.
    /// </summary>
    ISpiController? SPI { get; }

    /// <summary>
    /// OneWire controller for DS18B20, iButton, and other OneWire devices.
    /// </summary>
    IOneWireController? OneWire { get; }

    /// <summary>
    /// Connects to the board and initializes communication.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Disconnects from the board.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Resets the microcontroller.
    /// </summary>
    Task ResetAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets information about the board (free memory, chip model, etc.)
    /// </summary>
    Task<BoardInfo> GetInfoAsync(CancellationToken ct = default);
}

/// <summary>
/// Contains information about the connected board.
/// </summary>
public record BoardInfo(
    string ChipModel,
    int CpuFrequencyMHz,
    int FreeHeapBytes,
    int FlashSizeBytes,
    string SdkVersion
);
