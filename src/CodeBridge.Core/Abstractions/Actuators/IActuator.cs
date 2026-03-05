namespace CodeBridge.Core.Abstractions.Actuators;

/// <summary>
/// Base interface for all actuators (servos, motors, relays, LEDs, buzzers).
/// </summary>
public interface IActuator : IDisposable
{
    /// <summary>
    /// Human-readable actuator name (e.g., "SG90 Servo", "Relay CH1").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether the actuator has been initialized and is ready to use.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Initializes the actuator (sets pin modes, default state, etc.)
    /// </summary>
    Task InitAsync(CancellationToken ct = default);
}
