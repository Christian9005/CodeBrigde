using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// PIR HC-SR501 motion sensor — uses GPIO digital read.
/// Output: HIGH when motion detected, LOW when idle.
/// Connect OUT to any GPIO, VCC to 5V, GND to GND.
/// </summary>
public class ESP32PirSensor : IMotionSensor
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    public int Pin { get; }
    public string Name => $"PIR HC-SR501 (pin {Pin})";
    public string Unit => "motion";
    public bool IsReady => _initialized;

    public event EventHandler? MotionDetected;
    public event EventHandler? MotionEnded;

    public ESP32PirSensor(ITransport transport, int pin)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Pin = pin;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Set pin as INPUT
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PIN_MODE, Pin, (int)PinMode.Input), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"PIR init failed: {error}");
        _initialized = true;
    }

    public async Task<bool> ReadAsync(CancellationToken ct = default)
        => await IsMotionDetectedAsync(ct);

    public async Task<bool> IsMotionDetectedAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DIGITAL_READ, Pin), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"PIR read failed: {data}");
        return data.Trim() == "1";
    }

    public async IAsyncEnumerable<bool> ReadContinuousAsync(
        TimeSpan interval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        bool lastState = false;
        while (!ct.IsCancellationRequested)
        {
            bool current = await IsMotionDetectedAsync(ct);
            if (current && !lastState) MotionDetected?.Invoke(this, EventArgs.Empty);
            if (!current && lastState) MotionEnded?.Invoke(this, EventArgs.Empty);
            lastState = current;
            yield return current;
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
