using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// Analog gas sensor driver for MQ-series sensors (MQ-2, MQ-135, etc.).
/// Reads raw analog value and converts to approximate PPM using a calibration curve.
/// Connect AOUT to any ADC-capable GPIO (32-39 on ESP32).
/// </summary>
public class ESP32GasSensor : IGasSensor
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    /// <summary>Analog input pin (must be ADC-capable: GPIO 32-39).</summary>
    public int Pin { get; }

    /// <summary>Type of gas detected (e.g., "Smoke", "CO2", "VOC").</summary>
    public string GasType { get; }

    /// <summary>Clean air baseline ADC value (calibrate in fresh air).</summary>
    public double CleanAirValue { get; set; }

    /// <summary>Sensitivity factor for PPM conversion.</summary>
    public double SensitivityFactor { get; set; }

    public string Name => $"Gas Sensor ({GasType}, pin {Pin})";
    public string Unit => "ppm";
    public bool IsReady => _initialized;

    /// <summary>
    /// Creates an analog gas sensor driver.
    /// </summary>
    /// <param name="transport">Transport layer.</param>
    /// <param name="pin">ADC-capable GPIO pin (32-39).</param>
    /// <param name="gasType">Type of gas (e.g., "Smoke", "CO2").</param>
    /// <param name="cleanAirValue">Baseline ADC reading in clean air (default: 400).</param>
    /// <param name="sensitivityFactor">Conversion factor for PPM calculation (default: 5.0).</param>
    public ESP32GasSensor(ITransport transport, int pin, string gasType = "Smoke",
        double cleanAirValue = 400.0, double sensitivityFactor = 5.0)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Pin = pin;
        GasType = gasType;
        CleanAirValue = cleanAirValue;
        SensitivityFactor = sensitivityFactor;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Set pin as INPUT for analog reading
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PIN_MODE, Pin, (int)PinMode.Input), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Gas sensor init failed: {error}");
        _initialized = true;
    }

    /// <summary>
    /// Reads the raw ADC value (0-4095 on ESP32, 12-bit ADC).
    /// </summary>
    public async Task<int> ReadRawAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_ANALOG_READ, Pin), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Gas sensor read failed: {data}");
        return int.Parse(data.Trim(), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Reads gas concentration in approximate PPM.
    /// Uses a simple ratio-based formula: PPM = (raw / cleanAirValue - 1) * sensitivityFactor * 100.
    /// For accurate readings, calibrate with known gas concentrations.
    /// </summary>
    public async Task<double> ReadPpmAsync(CancellationToken ct = default)
    {
        var raw = await ReadRawAsync(ct);
        // Simple conversion: ratio of raw to baseline, scaled by sensitivity
        var ratio = raw / CleanAirValue;
        var ppm = Math.Max(0, (ratio - 1.0) * SensitivityFactor * 100.0);
        return Math.Round(ppm, 2);
    }

    public async Task<double> ReadAsync(CancellationToken ct = default)
        => await ReadPpmAsync(ct);

    public async IAsyncEnumerable<double> ReadContinuousAsync(
        TimeSpan interval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return await ReadPpmAsync(ct);
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
