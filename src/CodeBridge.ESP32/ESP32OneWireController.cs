using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 OneWire controller implementation.
/// Communicates with OneWire devices (DS18B20 temp sensors, etc.)
/// via the firmware's OneWire command handlers.
/// </summary>
public class ESP32OneWireController : IOneWireController
{
    private readonly ITransport _transport;
    private bool _disposed;

    public ESP32OneWireController(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task<string[]> ScanAsync(int pin, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_SCAN, pin), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OneWire scan failed: {data}");

        if (string.IsNullOrEmpty(data))
            return Array.Empty<string>();

        return data.Split(',');
    }

    public async Task<byte[]> ReadAsync(int pin, string address, int length, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_READ, pin, address, length), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OneWire read failed: {data}");

        return Convert.FromHexString(data);
    }

    public async Task WriteAsync(int pin, string address, byte[] data, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var hexData = Convert.ToHexString(data);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_WRITE, pin, address, hexData), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OneWire write failed: {error}");
    }

    public async Task<double> ReadTemperatureAsync(int pin, string? address = null, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string response;
        if (string.IsNullOrEmpty(address))
        {
            response = await _transport.SendCommandAsync(
                BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_TEMP, pin), ct);
        }
        else
        {
            response = await _transport.SendCommandAsync(
                BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_TEMP, pin, address), ct);
        }

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Temperature read failed: {data}");

        return double.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
    }

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
