using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Protocol;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// INA219 current/voltage/power sensor driver (I2C).
/// Measures current (±3.2A), bus voltage (0-26V), and power.
/// Default I2C address: 0x40 (configurable via A0/A1 pins: 0x40-0x4F).
/// </summary>
public class ESP32Ina219Sensor : ICurrentSensor
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    public int Address { get; }
    public string Name => $"INA219 (0x{Address:X2})";
    public string Unit => "mA";
    public bool IsReady => _initialized;

    /// <summary>
    /// Creates an INA219 current sensor driver.
    /// </summary>
    /// <param name="transport">Transport layer.</param>
    /// <param name="address">I2C address (default: 0x40).</param>
    public ESP32Ina219Sensor(ITransport transport, int address = 0x40)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Address = address;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_INA219_INIT, Address), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"INA219 init failed: {error}");
        _initialized = true;
    }

    /// <summary>
    /// Reads all power metrics in a single I2C burst: current, voltage, power, shunt voltage.
    /// </summary>
    public async Task<(double CurrentMa, double VoltageV, double PowerMw, double ShuntMv)> ReadAllAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_INA219_READ), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"INA219 read failed: {data}");

        // Format: "currentMa,voltageV,powerMw,shuntMv"
        var parts = data.Split(',');
        if (parts.Length < 4)
            throw new InvalidOperationException($"Invalid INA219 response: {data}");

        var current = double.Parse(parts[0], CultureInfo.InvariantCulture);
        var voltage = double.Parse(parts[1], CultureInfo.InvariantCulture);
        var power = double.Parse(parts[2], CultureInfo.InvariantCulture);
        var shunt = double.Parse(parts[3], CultureInfo.InvariantCulture);

        return (current, voltage, power, shunt);
    }

    public async Task<double> ReadCurrentMaAsync(CancellationToken ct = default)
    {
        var (current, _, _, _) = await ReadAllAsync(ct);
        return current;
    }

    public async Task<double> ReadVoltageAsync(CancellationToken ct = default)
    {
        var (_, voltage, _, _) = await ReadAllAsync(ct);
        return voltage;
    }

    public async Task<double> ReadPowerMwAsync(CancellationToken ct = default)
    {
        var (_, _, power, _) = await ReadAllAsync(ct);
        return power;
    }

    /// <summary>Returns the shunt voltage in millivolts.</summary>
    public async Task<double> ReadShuntVoltageMvAsync(CancellationToken ct = default)
    {
        var (_, _, _, shunt) = await ReadAllAsync(ct);
        return shunt;
    }

    /// <summary>Returns a combined PowerReading struct.</summary>
    public async Task<PowerReading> ReadPowerReadingAsync(CancellationToken ct = default)
    {
        var (current, voltage, power, _) = await ReadAllAsync(ct);
        return new PowerReading(current, voltage, power);
    }

    public async Task<PowerReading> ReadAsync(CancellationToken ct = default)
        => await ReadPowerReadingAsync(ct);

    public async IAsyncEnumerable<PowerReading> ReadContinuousAsync(
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
