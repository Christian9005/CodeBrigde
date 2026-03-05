using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Protocol;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// TCS34725 RGB color sensor driver (I2C).
/// Measures red, green, blue, and clear light channels, color temperature, and lux.
/// Default I2C address: 0x29 (fixed, not configurable).
/// </summary>
public class ESP32Tcs34725Sensor : IColorSensor
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    /// <summary>Integration time in milliseconds (24, 50, 101, 154, 600).</summary>
    public int IntegrationTimeMs { get; }

    /// <summary>Gain multiplier (1, 4, 16, 60).</summary>
    public int Gain { get; }

    public string Name => "TCS34725 Color Sensor";
    public string Unit => "RGB";
    public bool IsReady => _initialized;

    /// <summary>
    /// Creates a TCS34725 color sensor driver.
    /// </summary>
    /// <param name="transport">Transport layer.</param>
    /// <param name="integrationTimeMs">Integration time: 24, 50, 101, 154, or 600 ms (default: 50).</param>
    /// <param name="gain">Gain: 1, 4, 16, or 60x (default: 4).</param>
    public ESP32Tcs34725Sensor(ITransport transport, int integrationTimeMs = 50, int gain = 4)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        IntegrationTimeMs = integrationTimeMs;
        Gain = gain;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_TCS34725_INIT, IntegrationTimeMs, Gain), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"TCS34725 init failed: {error}");
        _initialized = true;
    }

    /// <summary>
    /// Reads all values in a single I2C read: R, G, B (0-255), clear channel, color temp, lux.
    /// </summary>
    private async Task<(byte R, byte G, byte B, ushort Clear, int ColorTemp, double Lux)> ReadRawAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_TCS34725_READ), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"TCS34725 read failed: {data}");

        // Format: "r,g,b,clear,colorTemp,lux"
        var parts = data.Split(',');
        if (parts.Length < 6)
            throw new InvalidOperationException($"Invalid TCS34725 response: {data}");

        var r = byte.Parse(parts[0], CultureInfo.InvariantCulture);
        var g = byte.Parse(parts[1], CultureInfo.InvariantCulture);
        var b = byte.Parse(parts[2], CultureInfo.InvariantCulture);
        var clear = ushort.Parse(parts[3], CultureInfo.InvariantCulture);
        var colorTemp = int.Parse(parts[4], CultureInfo.InvariantCulture);
        var lux = double.Parse(parts[5], CultureInfo.InvariantCulture);

        return (r, g, b, clear, colorTemp, lux);
    }

    public async Task<RgbColor> ReadColorAsync(CancellationToken ct = default)
    {
        var (r, g, b, clear, _, _) = await ReadRawAsync(ct);
        return new RgbColor(r, g, b, clear);
    }

    public async Task<int> ReadColorTemperatureAsync(CancellationToken ct = default)
    {
        var (_, _, _, _, colorTemp, _) = await ReadRawAsync(ct);
        return colorTemp;
    }

    /// <summary>Reads the light intensity in lux.</summary>
    public async Task<double> ReadLuxAsync(CancellationToken ct = default)
    {
        var (_, _, _, _, _, lux) = await ReadRawAsync(ct);
        return lux;
    }

    public async Task<RgbColor> ReadAsync(CancellationToken ct = default)
        => await ReadColorAsync(ct);

    public async IAsyncEnumerable<RgbColor> ReadContinuousAsync(
        TimeSpan interval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return await ReadColorAsync(ct);
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
