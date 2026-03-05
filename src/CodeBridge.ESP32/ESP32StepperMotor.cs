using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Actuators;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 stepper motor driver via 4-wire half-step sequence.
/// Compatible with 28BYJ-48 + ULN2003, NEMA17 + A4988/DRV8825 (in 4-wire mode).
/// Stepping is blocking on the firmware side.
/// </summary>
public class ESP32StepperMotor : IStepperMotor
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;
    private double _speedRpm = 10;

    /// <summary>Stepper pin 1 (used as identifier).</summary>
    public int Pin1 { get; }

    /// <summary>Stepper pin 2.</summary>
    public int Pin2 { get; }

    /// <summary>Stepper pin 3.</summary>
    public int Pin3 { get; }

    /// <summary>Stepper pin 4.</summary>
    public int Pin4 { get; }

    public string Name => $"Stepper (pin1={Pin1})";
    public bool IsReady => _initialized;
    public int StepsPerRevolution { get; }

    public ESP32StepperMotor(ITransport transport, int pin1, int pin2, int pin3, int pin4, int stepsPerRevolution = 2048)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Pin1 = pin1;
        Pin2 = pin2;
        Pin3 = pin3;
        Pin4 = pin4;
        StepsPerRevolution = stepsPerRevolution > 0 ? stepsPerRevolution : 2048;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_STEPPER_INIT, Pin1, Pin2, Pin3, Pin4, StepsPerRevolution), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Stepper init failed: {error}");

        _initialized = true;
    }

    /// <summary>
    /// Move a specific number of steps. Negative = reverse.
    /// This is a blocking call (firmware executes all steps before responding).
    /// </summary>
    public async Task StepAsync(int steps, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_STEPPER_STEP, Pin1, steps, (int)_speedRpm), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Stepper step failed: {error}");
    }

    public async Task RotateDegreesAsync(double degrees, CancellationToken ct = default)
    {
        int steps = (int)(degrees / 360.0 * StepsPerRevolution);
        await StepAsync(steps, ct);
    }

    public Task SetSpeedRpmAsync(double rpm, CancellationToken ct = default)
    {
        if (rpm <= 0)
            throw new ArgumentOutOfRangeException(nameof(rpm), "Speed must be positive.");

        _speedRpm = rpm;
        return Task.CompletedTask;
    }

    public async Task ReleaseAsync(CancellationToken ct = default)
    {
        // Step 0 steps to trigger the coil-release in firmware
        await StepAsync(0, ct);
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
