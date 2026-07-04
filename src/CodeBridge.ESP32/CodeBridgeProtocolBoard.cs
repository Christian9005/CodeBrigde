using System.Text.Json;
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// Generic board implementation for CodeBridge firmware that speaks the common
/// line protocol but does not expose ESP32-specific runtime capabilities.
/// </summary>
public sealed class CodeBridgeProtocolBoard : IBoard, IAsyncDisposable
{
    private readonly ITransport _transport;
    private readonly BoardFamily _family;
    private ESP32GpioController? _gpio;
    private ESP32I2cController? _i2c;
    private ESP32SpiController? _spi;

    public CodeBridgeProtocolBoard(ITransport transport, string name, BoardFamily family)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Name = string.IsNullOrWhiteSpace(name) ? "CodeBridge Board" : name;
        _family = family;
    }

    public string Name { get; }
    public BoardFamily Family => _family;
    public string FirmwareVersion { get; private set; } = "unknown";
    public bool IsConnected => _transport.IsConnected;
    public ITransport Transport => _transport;
    public IGpioController Gpio => _gpio ?? throw new InvalidOperationException("Board not connected.");
    public II2cController I2C => _i2c ?? throw new InvalidOperationException("Board not connected.");
    public ISpiController? SPI => _spi;
    public IOneWireController? OneWire => null;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await _transport.ConnectAsync(ct);

        _gpio = new ESP32GpioController(_transport);
        _i2c = new ESP32I2cController(_transport);
        _spi = new ESP32SpiController(_transport);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PING), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success || data != "PONG")
            throw new InvalidOperationException($"{Name} ping failed: {data}");

        var versionResponse = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_VERSION), ct);
        var (versionSuccess, version) = BridgeProtocol.ParseResponse(versionResponse);
        if (versionSuccess)
            FirmwareVersion = version;
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await _transport.DisconnectAsync(ct);
        _gpio = null;
        _i2c = null;
        _spi = null;
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

        var info = JsonSerializer.Deserialize<BoardInfoDto>(data);
        if (info is null)
            throw new InvalidOperationException("Invalid board info response.");

        return new BoardInfo(
            info.chip ?? Name,
            info.freq,
            info.heap,
            info.flash,
            info.sdk ?? "unknown");
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

    private record BoardInfoDto(string? chip, int freq, int heap, int flash, string? sdk);
}
