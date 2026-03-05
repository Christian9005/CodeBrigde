using CodeBridge.Core.Enums;

namespace CodeBridge.Core.Abstractions.Actuators;

/// <summary>
/// Controls a servo motor (standard 0-180° or continuous rotation).
/// Implemented via PWM signal (typically 50Hz, 500-2500µs pulse).
/// Works with: SG90, MG996R, MG90S, and most hobby servos.
/// </summary>
public interface IServo : IActuator
{
    /// <summary>
    /// The GPIO pin this servo is attached to.
    /// </summary>
    int Pin { get; }

    /// <summary>
    /// Minimum angle in degrees (usually 0).
    /// </summary>
    int MinAngle { get; }

    /// <summary>
    /// Maximum angle in degrees (usually 180).
    /// </summary>
    int MaxAngle { get; }

    /// <summary>
    /// Moves the servo to a specific angle (0-180° for standard servos).
    /// </summary>
    Task SetAngleAsync(int degrees, CancellationToken ct = default);

    /// <summary>
    /// Gets the current angle of the servo.
    /// </summary>
    Task<int> GetAngleAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the raw PWM pulse width in microseconds (500-2500 typical).
    /// Useful for fine-tuning or continuous rotation servos.
    /// </summary>
    Task SetPulseWidthAsync(int microseconds, CancellationToken ct = default);

    /// <summary>
    /// Detaches the servo (stops sending PWM signal, allows free rotation).
    /// </summary>
    Task DetachAsync(CancellationToken ct = default);
}

/// <summary>
/// Controls a DC motor via an H-Bridge driver.
/// Works with: L298N, L293D, TB6612FNG.
/// </summary>
public interface IMotorController : IActuator
{
    /// <summary>
    /// Sets the motor speed (-100 to 100). Negative = reverse.
    /// </summary>
    Task SetSpeedAsync(int speedPercent, CancellationToken ct = default);

    /// <summary>
    /// Stops the motor immediately (brake).
    /// </summary>
    Task BrakeAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops the motor by coasting (no brake, free spin).
    /// </summary>
    Task CoastAsync(CancellationToken ct = default);

    /// <summary>
    /// Current direction of the motor.
    /// </summary>
    MotorDirection Direction { get; }
}

/// <summary>
/// Controls a stepper motor via a driver board.
/// Works with: 28BYJ-48 + ULN2003, NEMA17 + A4988/DRV8825.
/// </summary>
public interface IStepperMotor : IActuator
{
    /// <summary>
    /// Steps per full revolution (e.g., 2048 for 28BYJ-48, 200 for NEMA17).
    /// </summary>
    int StepsPerRevolution { get; }

    /// <summary>
    /// Moves a specific number of steps. Negative = reverse.
    /// </summary>
    Task StepAsync(int steps, CancellationToken ct = default);

    /// <summary>
    /// Rotates a specific number of degrees. Negative = reverse.
    /// </summary>
    Task RotateDegreesAsync(double degrees, CancellationToken ct = default);

    /// <summary>
    /// Sets the speed in RPM (revolutions per minute).
    /// </summary>
    Task SetSpeedRpmAsync(double rpm, CancellationToken ct = default);

    /// <summary>
    /// Releases the motor coils (allows free rotation, saves power).
    /// </summary>
    Task ReleaseAsync(CancellationToken ct = default);
}

/// <summary>
/// Controls a relay (single or multi-channel).
/// </summary>
public interface IRelay : IActuator
{
    /// <summary>
    /// Whether the relay is currently energized (ON).
    /// </summary>
    bool IsOn { get; }

    /// <summary>
    /// The GPIO pin controlling this relay.
    /// </summary>
    int Pin { get; }

    /// <summary>
    /// Turns the relay ON (closes the circuit).
    /// </summary>
    Task OnAsync(CancellationToken ct = default);

    /// <summary>
    /// Turns the relay OFF (opens the circuit).
    /// </summary>
    Task OffAsync(CancellationToken ct = default);

    /// <summary>
    /// Toggles the relay state.
    /// </summary>
    Task ToggleAsync(CancellationToken ct = default);

    /// <summary>
    /// Turns ON for a duration, then automatically turns OFF.
    /// </summary>
    Task PulseAsync(TimeSpan duration, CancellationToken ct = default);
}

/// <summary>
/// Controls an addressable LED strip (WS2812B, SK6812, NeoPixel).
/// Each LED can be individually addressed with RGB(W) color values.
/// </summary>
public interface ILedStrip : IActuator
{
    /// <summary>
    /// Total number of LEDs in the strip.
    /// </summary>
    int LedCount { get; }

    /// <summary>
    /// The GPIO data pin.
    /// </summary>
    int Pin { get; }

    /// <summary>
    /// Sets the color of a specific LED by index.
    /// </summary>
    Task SetPixelAsync(int index, byte r, byte g, byte b, CancellationToken ct = default);

    /// <summary>
    /// Sets all LEDs to the same color.
    /// </summary>
    Task SetAllAsync(byte r, byte g, byte b, CancellationToken ct = default);

    /// <summary>
    /// Turns off all LEDs.
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Pushes the pixel buffer to the strip (makes changes visible).
    /// </summary>
    Task ShowAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the global brightness (0-255).
    /// </summary>
    Task SetBrightnessAsync(byte brightness, CancellationToken ct = default);

    /// <summary>
    /// Sets a range of pixels from a color array.
    /// </summary>
    Task SetRangeAsync(int startIndex, (byte R, byte G, byte B)[] colors, CancellationToken ct = default);
}

/// <summary>
/// Controls an RGB LED (common cathode or anode) via 3 PWM pins.
/// </summary>
public interface IRgbLed : IActuator
{
    /// <summary>
    /// Sets the RGB color (0-255 per channel).
    /// </summary>
    Task SetColorAsync(byte r, byte g, byte b, CancellationToken ct = default);

    /// <summary>
    /// Turns off the LED.
    /// </summary>
    Task OffAsync(CancellationToken ct = default);

    /// <summary>
    /// Fades to a color over a duration.
    /// </summary>
    Task FadeToAsync(byte r, byte g, byte b, TimeSpan duration, CancellationToken ct = default);
}

/// <summary>
/// Controls a buzzer or piezo speaker.
/// </summary>
public interface IBuzzer : IActuator
{
    /// <summary>
    /// The GPIO pin for the buzzer.
    /// </summary>
    int Pin { get; }

    /// <summary>
    /// Plays a tone at the specified frequency for a given duration.
    /// </summary>
    Task ToneAsync(int frequencyHz, TimeSpan duration, CancellationToken ct = default);

    /// <summary>
    /// Stops any currently playing tone.
    /// </summary>
    Task NoToneAsync(CancellationToken ct = default);

    /// <summary>
    /// Plays a short beep (1000Hz, 100ms).
    /// </summary>
    Task BeepAsync(CancellationToken ct = default);

    /// <summary>
    /// Plays a melody defined as (frequency, duration) pairs.
    /// </summary>
    Task PlayMelodyAsync(IEnumerable<(int FrequencyHz, TimeSpan Duration)> notes, CancellationToken ct = default);
}
