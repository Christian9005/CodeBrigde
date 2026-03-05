using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Protocol;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// DS18B20 OneWire digital temperature sensor.
/// Wraps existing OneWire temperature command (OWT).
/// Connect DQ to GPIO with 4.7kΩ pull-up resistor.
/// </summary>
public class ESP32Ds18b20Sensor : ITemperatureSensor
{
    private readonly ITransport _transport;
    private bool _disposed;

    public int Pin { get; }
    public string Name => $"DS18B20 (pin {Pin})";
    public string Unit => "°C";
    public bool IsReady => true; // OWT handles init internally

    public ESP32Ds18b20Sensor(ITransport transport, int pin)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Pin = pin;
    }

    /// <summary>DS18B20 uses existing OneWire scan, no separate init needed.</summary>
    public Task InitAsync(CancellationToken ct = default) => Task.CompletedTask;

    public async Task<double> ReadAsync(CancellationToken ct = default)
        => await ReadTemperatureCelsiusAsync(ct);

    public async Task<double> ReadTemperatureCelsiusAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_TEMP, Pin), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"DS18B20 read failed: {data}");

        // OWT returns comma-separated temps for all sensors on bus, take first
        var firstTemp = data.Split(',')[0];
        return double.Parse(firstTemp, CultureInfo.InvariantCulture);
    }

    public async Task<double> ReadTemperatureFahrenheitAsync(CancellationToken ct = default)
    {
        var tempC = await ReadTemperatureCelsiusAsync(ct);
        return tempC * 9.0 / 5.0 + 32.0;
    }

    public async IAsyncEnumerable<double> ReadContinuousAsync(
        TimeSpan interval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var minInterval = TimeSpan.FromSeconds(1);
        var actualInterval = interval < minInterval ? minInterval : interval;
        while (!ct.IsCancellationRequested)
        {
            yield return await ReadTemperatureCelsiusAsync(ct);
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
