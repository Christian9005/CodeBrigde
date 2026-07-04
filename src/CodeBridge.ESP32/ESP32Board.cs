using System.Text.Json;
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Acquisition;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 board implementation. Communicates with an ESP32 running
/// the CodeBridge firmware via a transport layer (Serial/WiFi).
/// </summary>
public class ESP32Board : IBoard, IBoardWithAcquisition, IAsyncDisposable
{
    private readonly ITransport _transport;
    private ESP32GpioController? _gpio;
    private ESP32I2cController? _i2c;
    private ESP32SpiController? _spi;
    private ESP32OneWireController? _oneWire;
    private ESP32InterruptController? _interrupts;
    private ESP32WatchdogController? _watchdog;
    private ESP32PowerController? _power;
    private ESP32OtaController? _ota;
    private ESP32MqttClient? _mqtt;
    private ESP32AcquisitionController? _acquisition;

    public string Name { get; private set; } = "ESP32";
    public BoardFamily Family => BoardFamily.ESP32;
    public string FirmwareVersion { get; private set; } = "unknown";
    public bool IsConnected => _transport.IsConnected;
    public ITransport Transport => _transport;
    public IGpioController Gpio => _gpio ?? throw new InvalidOperationException("Board not connected.");
    public II2cController I2C => _i2c ?? throw new InvalidOperationException("Board not connected.");
    public ISpiController? SPI => _spi;
    public IOneWireController? OneWire => _oneWire;

    /// <summary>GPIO interrupt controller for edge detection.</summary>
    public ESP32InterruptController Interrupts => _interrupts ?? throw new InvalidOperationException("Board not connected.");

    /// <summary>Watchdog timer controller.</summary>
    public ESP32WatchdogController Watchdog => _watchdog ?? throw new InvalidOperationException("Board not connected.");

    /// <summary>Power management (deep sleep).</summary>
    public ESP32PowerController Power => _power ?? throw new InvalidOperationException("Board not connected.");

    /// <summary>OTA firmware update controller.</summary>
    public ESP32OtaController Ota => _ota ?? throw new InvalidOperationException("Board not connected.");

    /// <summary>MQTT publish/subscribe client.</summary>
    public ESP32MqttClient Mqtt => _mqtt ?? throw new InvalidOperationException("Board not connected.");

    /// <summary>Firmware-buffered acquisition controller.</summary>
    public IBoardAcquisitionController Acquisition => _acquisition ?? throw new InvalidOperationException("Board not connected.");

    public ESP32Board(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await _transport.ConnectAsync(ct);

        // Initialize controllers
        _gpio = new ESP32GpioController(_transport);
        _i2c = new ESP32I2cController(_transport);
        _spi = new ESP32SpiController(_transport);
        _oneWire = new ESP32OneWireController(_transport);
        _interrupts = new ESP32InterruptController(_transport);
        _watchdog = new ESP32WatchdogController(_transport);
        _power = new ESP32PowerController(_transport);
        _ota = new ESP32OtaController(_transport);
        _mqtt = new ESP32MqttClient(_transport);
        _acquisition = new ESP32AcquisitionController(_transport);

        // Verify connection with a ping
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PING), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success || data != "PONG")
            throw new InvalidOperationException($"ESP32 ping failed: {data}");

        // Get firmware version
        var verResponse = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_VERSION), ct);
        var (verSuccess, version) = BridgeProtocol.ParseResponse(verResponse);
        if (verSuccess)
            FirmwareVersion = version;
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await _transport.DisconnectAsync(ct);
        _gpio = null;
        _i2c = null;
        _spi = null;
        _oneWire = null;
        _interrupts = null;
        _watchdog = null;
        _power = null;
        _ota = null;
        _mqtt = null;
        _acquisition = null;
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_RESET), ct);
    }

    public async Task<BoardInfo> GetInfoAsync(CancellationToken ct = default)
    {
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_INFO), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Failed to get board info: {data}");

        // Parse JSON response from firmware
        var info = JsonSerializer.Deserialize<BoardInfoDto>(data);
        if (info is null)
            throw new InvalidOperationException("Invalid board info response.");

        return new BoardInfo(
            info.chip ?? "ESP32",
            info.freq,
            info.heap,
            info.flash,
            info.sdk ?? "unknown"
        );
    }

    public void Dispose()
    {
        _transport.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        Dispose();
    }

    // Internal DTO for JSON deserialization
    private record BoardInfoDto(string? chip, int freq, int heap, int flash, string? sdk);
}
