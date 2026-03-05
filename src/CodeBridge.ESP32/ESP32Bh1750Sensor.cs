using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Protocol;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// BH1750 I2C digital ambient light sensor.
/// Default I2C address: 0x23 (alternative: 0x5C with ADDR pin HIGH).
/// Measures 1–65535 lux with 1 lux resolution.
/// </summary>
public class ESP32Bh1750Sensor : ILightSensor
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    public int Address { get; }
    public string Name => $"BH1750 (0x{Address:X2})";
    public string Unit => "lux";
    public bool IsReady => _initialized;

    public ESP32Bh1750Sensor(ITransport transport, int address = 0x23)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Address = address;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_BH1750_INIT, Address), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"BH1750 init failed: {error}");
        _initialized = true;
    }

    public async Task<double> ReadAsync(CancellationToken ct = default)
        => await ReadLuxAsync(ct);

    public async Task<double> ReadLuxAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_BH1750_READ), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"BH1750 read failed: {data}");
        return double.Parse(data, CultureInfo.InvariantCulture);
    }

    public async IAsyncEnumerable<double> ReadContinuousAsync(
        TimeSpan interval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return await ReadLuxAsync(ct);
            await Task.Delay(interval, ct);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
