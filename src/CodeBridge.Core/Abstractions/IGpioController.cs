using CodeBridge.Core.Enums;

namespace CodeBridge.Core.Abstractions;

/// <summary>
/// Represents a GPIO (General Purpose Input/Output) controller.
/// This is the fundamental building block for interacting with hardware pins.
/// </summary>
public interface IGpioController : IDisposable
{
    /// <summary>
    /// Sets the mode of a specific pin.
    /// </summary>
    Task SetPinModeAsync(int pin, PinMode mode, CancellationToken ct = default);

    /// <summary>
    /// Writes a digital value (High/Low) to a pin.
    /// </summary>
    Task DigitalWriteAsync(int pin, PinValue value, CancellationToken ct = default);

    /// <summary>
    /// Reads the digital value from a pin.
    /// </summary>
    Task<PinValue> DigitalReadAsync(int pin, CancellationToken ct = default);

    /// <summary>
    /// Reads an analog value (0-4095 for ESP32, 0-1023 for Arduino).
    /// </summary>
    Task<int> AnalogReadAsync(int pin, CancellationToken ct = default);

    /// <summary>
    /// Writes a PWM value to a pin (0-255 duty cycle).
    /// </summary>
    Task PwmWriteAsync(int pin, int dutyCycle, int frequency = 5000, CancellationToken ct = default);
}
