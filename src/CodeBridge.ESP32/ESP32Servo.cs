using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Actuators;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 servo driver. Supports up to 8 simultaneous servos via ESP32Servo library.
/// Default pulse range: 500-2500µs. Compatible with SG90, MG996R, MG90S, etc.
/// </summary>
public class ESP32Servo : IServo
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _attached;

    public string Name => $"Servo (pin {Pin})";
    public bool IsReady => _attached;
    public int Pin { get; }
    public int MinAngle => 0;
    public int MaxAngle => 180;

    /// <summary>
    /// Minimum pulse width in microseconds (default: 500).
    /// </summary>
    public int MinPulseUs { get; }

    /// <summary>
    /// Maximum pulse width in microseconds (default: 2500).
    /// </summary>
    public int MaxPulseUs { get; }

    public ESP32Servo(ITransport transport, int pin, int minPulseUs = 500, int maxPulseUs = 2500)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Pin = pin;
        MinPulseUs = minPulseUs;
        MaxPulseUs = maxPulseUs;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_ATTACH, Pin, MinPulseUs, MaxPulseUs), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Servo attach failed: {error}");

        _attached = true;
    }

    public async Task SetAngleAsync(int degrees, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (degrees < MinAngle || degrees > MaxAngle)
            throw new ArgumentOutOfRangeException(nameof(degrees), $"Angle must be {MinAngle}-{MaxAngle}.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_WRITE, Pin, degrees), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Servo write failed: {error}");
    }

    public async Task<int> GetAngleAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_READ, Pin), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Servo read failed: {data}");

        return int.Parse(data);
    }

    public async Task SetPulseWidthAsync(int microseconds, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_US, Pin, microseconds), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Servo microseconds failed: {error}");
    }

    public async Task DetachAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_DETACH, Pin), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Servo detach failed: {error}");

        _attached = false;
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
