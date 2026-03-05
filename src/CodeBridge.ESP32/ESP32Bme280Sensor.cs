using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Protocol;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// BME280 I2C environmental sensor — temperature, humidity, and barometric pressure.
/// Default I2C address: 0x76 (alternative: 0x77).
/// </summary>
public class ESP32Bme280Sensor : ITemperatureSensor, IHumiditySensor, IPressureSensor
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    public int Address { get; }
    public string Name => $"BME280 (0x{Address:X2})";
    public string Unit => "°C";
    public bool IsReady => _initialized;

    public ESP32Bme280Sensor(ITransport transport, int address = 0x76)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Address = address;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_BME280_INIT, Address), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"BME280 init failed: {error}");
        _initialized = true;
    }

    /// <summary>Reads temperature, humidity, and pressure in one I2C transaction.</summary>
    public async Task<EnvironmentReading> ReadAllAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_BME280_READ), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"BME280 read failed: {data}");

        // Format: "temp,humidity,pressure"
        var parts = data.Split(',');
        if (parts.Length < 3)
            throw new InvalidOperationException($"Invalid BME280 response: {data}");

        var temp = double.Parse(parts[0], CultureInfo.InvariantCulture);
        var hum = double.Parse(parts[1], CultureInfo.InvariantCulture);
        var pres = double.Parse(parts[2], CultureInfo.InvariantCulture);
        return new EnvironmentReading(temp, hum, pres);
    }

    public async Task<double> ReadAsync(CancellationToken ct = default)
    {
        var reading = await ReadAllAsync(ct);
        return reading.TemperatureCelsius;
    }

    public async Task<double> ReadTemperatureCelsiusAsync(CancellationToken ct = default)
        => (await ReadAllAsync(ct)).TemperatureCelsius;

    public async Task<double> ReadTemperatureFahrenheitAsync(CancellationToken ct = default)
        => (await ReadAllAsync(ct)).TemperatureFahrenheit;

    public async Task<double> ReadHumidityAsync(CancellationToken ct = default)
        => (await ReadAllAsync(ct)).HumidityPercent ?? 0;

    public async Task<double> ReadPressureHpaAsync(CancellationToken ct = default)
        => (await ReadAllAsync(ct)).PressureHpa ?? 0;

    public async Task<double> ReadAltitudeMetersAsync(double seaLevelHpa = 1013.25, CancellationToken ct = default)
    {
        var pressure = await ReadPressureHpaAsync(ct);
        // Hypsometric formula
        return 44330.0 * (1.0 - Math.Pow(pressure / seaLevelHpa, 0.1903));
    }

    public async IAsyncEnumerable<double> ReadContinuousAsync(
        TimeSpan interval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return await ReadAsync(ct);
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
