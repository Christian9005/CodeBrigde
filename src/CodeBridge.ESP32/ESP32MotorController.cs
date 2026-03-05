using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Actuators;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 DC motor driver via H-Bridge (L298N, L293D, TB6612FNG).
/// Uses two direction pins (IN1, IN2) and an optional enable pin (EN) for PWM speed control.
/// </summary>
public class ESP32MotorController : IMotorController
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    /// <summary>IN1 direction pin.</summary>
    public int In1Pin { get; }

    /// <summary>IN2 direction pin.</summary>
    public int In2Pin { get; }

    /// <summary>Enable pin for PWM speed control (-1 if not used).</summary>
    public int EnablePin { get; }

    public string Name => $"Motor (IN1={In1Pin}, IN2={In2Pin})";
    public bool IsReady => _initialized;
    public MotorDirection Direction { get; private set; } = MotorDirection.Stopped;

    public ESP32MotorController(ITransport transport, int in1Pin, int in2Pin, int enablePin = -1)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        In1Pin = in1Pin;
        In2Pin = in2Pin;
        EnablePin = enablePin;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MOTOR_INIT, In1Pin, In2Pin, EnablePin), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Motor init failed: {error}");

        _initialized = true;
    }

    /// <summary>
    /// Sets motor speed. Range: -100 (full reverse) to 100 (full forward). 0 = stop.
    /// </summary>
    public async Task SetSpeedAsync(int speedPercent, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        speedPercent = Math.Clamp(speedPercent, -100, 100);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MOTOR_SPEED, In1Pin, speedPercent), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Motor speed failed: {error}");

        Direction = speedPercent > 0 ? MotorDirection.Forward
                  : speedPercent < 0 ? MotorDirection.Reverse
                  : MotorDirection.Stopped;
    }

    public async Task BrakeAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MOTOR_STOP, In1Pin), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Motor stop failed: {error}");

        Direction = MotorDirection.Stopped;
    }

    public Task CoastAsync(CancellationToken ct = default)
    {
        // Coast is same as brake in simple H-Bridge mode (both LOW)
        return BrakeAsync(ct);
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
