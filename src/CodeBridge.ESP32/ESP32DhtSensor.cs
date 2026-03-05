using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Protocol;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 DHT temperature/humidity sensor driver (DHT11, DHT22/AM2302).
/// Uses bit-bang timing on the firmware side (no library dependency).
/// Minimum read interval: ~2s.
/// </summary>
public class ESP32DhtSensor : ITemperatureSensor, IHumiditySensor
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    /// <summary>
    /// The GPIO data pin connected to the DHT sensor.
    /// </summary>
    public int Pin { get; }

    /// <summary>
    /// DHT sensor type: 11 for DHT11, 22 for DHT22/AM2302.
    /// </summary>
    public int DhtType { get; }

    public string Name => $"DHT{DhtType} (pin {Pin})";
    public string Unit => "°C";
    public bool IsReady => _initialized;

    public ESP32DhtSensor(ITransport transport, int pin, int dhtType = 22)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Pin = pin;

        if (dhtType != 11 && dhtType != 22)
            throw new ArgumentException("DHT type must be 11 or 22.", nameof(dhtType));

        DhtType = dhtType;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Initialize the DHT pin
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DHT_INIT, Pin, DhtType), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"DHT init failed: {error}");

        _initialized = true;
    }

    /// <summary>
    /// Reads both temperature and humidity from the DHT sensor.
    /// Returns (temperature °C, humidity %).
    /// </summary>
    public async Task<(double TemperatureC, double HumidityPercent)> ReadBothAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DHT_READ, Pin, DhtType), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"DHT read failed: {data}");

        // Response format: "temp,humidity" e.g. "23.50,65.00"
        var parts = data.Split(',');
        if (parts.Length < 2)
            throw new InvalidOperationException($"Invalid DHT response format: {data}");

        var temp = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
        var humidity = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);

        return (temp, humidity);
    }

    public async Task<double> ReadAsync(CancellationToken ct = default)
    {
        var (temp, _) = await ReadBothAsync(ct);
        return temp;
    }

    public async Task<double> ReadTemperatureCelsiusAsync(CancellationToken ct = default)
    {
        var (temp, _) = await ReadBothAsync(ct);
        return temp;
    }

    public async Task<double> ReadTemperatureFahrenheitAsync(CancellationToken ct = default)
    {
        var tempC = await ReadTemperatureCelsiusAsync(ct);
        return tempC * 9.0 / 5.0 + 32.0;
    }

    public async Task<double> ReadHumidityAsync(CancellationToken ct = default)
    {
        var (_, humidity) = await ReadBothAsync(ct);
        return humidity;
    }

    public async IAsyncEnumerable<double> ReadContinuousAsync(
        TimeSpan interval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // DHT sensors need at least 2s between reads
        var minInterval = TimeSpan.FromSeconds(2);
        var actualInterval = interval < minInterval ? minInterval : interval;

        while (!ct.IsCancellationRequested)
        {
            yield return await ReadAsync(ct);
            await Task.Delay(actualInterval, ct);
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
