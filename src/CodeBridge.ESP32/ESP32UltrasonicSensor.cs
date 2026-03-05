using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Protocol;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 HC-SR04 ultrasonic distance sensor driver.
/// Measures distance via pulse-echo timing on the firmware.
/// Range: 2-400 cm. Accuracy: ±3mm.
/// </summary>
public class ESP32UltrasonicSensor : IDistanceSensor
{
    private readonly ITransport _transport;
    private bool _disposed;

    /// <summary>
    /// The GPIO pin connected to the HC-SR04 TRIG pin.
    /// </summary>
    public int TrigPin { get; }

    /// <summary>
    /// The GPIO pin connected to the HC-SR04 ECHO pin.
    /// </summary>
    public int EchoPin { get; }

    public string Name => $"HC-SR04 (trig={TrigPin}, echo={EchoPin})";
    public string Unit => "cm";
    public bool IsReady => true; // No init needed
    public double MinRangeCm => 2.0;
    public double MaxRangeCm => 400.0;

    public ESP32UltrasonicSensor(ITransport transport, int trigPin, int echoPin)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        TrigPin = trigPin;
        EchoPin = echoPin;
    }

    public Task InitAsync(CancellationToken ct = default)
    {
        // HC-SR04 doesn't require initialization; pin modes set by firmware
        return Task.CompletedTask;
    }

    public async Task<double> ReadAsync(CancellationToken ct = default)
    {
        return await ReadDistanceCmAsync(ct);
    }

    public async Task<double> ReadDistanceCmAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_ULTRA_READ, TrigPin, EchoPin), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Ultrasonic read failed: {data}");

        return double.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async IAsyncEnumerable<double> ReadContinuousAsync(
        TimeSpan interval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return await ReadDistanceCmAsync(ct);
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
