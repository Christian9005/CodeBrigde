using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32-specific I2C controller implementation.
/// </summary>
internal class ESP32I2cController : II2cController
{
    private readonly ITransport _transport;

    public ESP32I2cController(ITransport transport)
    {
        _transport = transport;
    }

    public async Task<IReadOnlyList<byte>> ScanAsync(CancellationToken ct = default)
    {
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_I2C_SCAN), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"I2C scan failed: {data}");

        if (string.IsNullOrEmpty(data))
            return Array.Empty<byte>();

        return data.Split(',')
            .Select(s => byte.Parse(s.Trim()))
            .ToList()
            .AsReadOnly();
    }

    public async Task WriteAsync(byte address, byte[] data, CancellationToken ct = default)
    {
        var hexData = Convert.ToHexString(data);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_I2C_WRITE, address, hexData), ct);

        var (success, msg) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"I2C write to 0x{address:X2} failed: {msg}");
    }

    public async Task<byte[]> ReadAsync(byte address, int length, CancellationToken ct = default)
    {
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_I2C_READ, address, length), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"I2C read from 0x{address:X2} failed: {data}");

        return Convert.FromHexString(data);
    }

    public async Task WriteRegisterAsync(byte address, byte register, byte[] data, CancellationToken ct = default)
    {
        var hexData = Convert.ToHexString(data);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_I2C_WREG, address, register, hexData), ct);

        var (success, msg) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"I2C register write failed: {msg}");
    }

    public async Task<byte[]> ReadRegisterAsync(byte address, byte register, int length, CancellationToken ct = default)
    {
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_I2C_RREG, address, register, length), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"I2C register read failed: {data}");

        return Convert.FromHexString(data);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
