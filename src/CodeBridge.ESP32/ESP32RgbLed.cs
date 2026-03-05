using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Actuators;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// RGB LED driver using 3 PWM pins (common cathode or common anode).
/// Each channel (R, G, B) is driven via PWM (0-255 duty cycle).
/// </summary>
public class ESP32RgbLed : IRgbLed
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    public int RedPin { get; }
    public int GreenPin { get; }
    public int BluePin { get; }
    public bool CommonAnode { get; }
    public string Name => $"RGB LED (R:{RedPin}, G:{GreenPin}, B:{BluePin})";
    public bool IsReady => _initialized;

    /// <summary>
    /// Creates an RGB LED driver.
    /// </summary>
    /// <param name="transport">The transport layer.</param>
    /// <param name="redPin">GPIO pin for Red channel.</param>
    /// <param name="greenPin">GPIO pin for Green channel.</param>
    /// <param name="bluePin">GPIO pin for Blue channel.</param>
    /// <param name="commonAnode">True for common-anode LEDs (inverts PWM values).</param>
    public ESP32RgbLed(ITransport transport, int redPin, int greenPin, int bluePin, bool commonAnode = false)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        RedPin = redPin;
        GreenPin = greenPin;
        BluePin = bluePin;
        CommonAnode = commonAnode;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Set all 3 pins as OUTPUT
        foreach (var pin in new[] { RedPin, GreenPin, BluePin })
        {
            var response = await _transport.SendCommandAsync(
                BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PIN_MODE, pin, (int)PinMode.Output), ct);
            var (success, error) = BridgeProtocol.ParseResponse(response);
            if (!success) throw new InvalidOperationException($"RGB LED init pin {pin} failed: {error}");
        }

        // Start all channels at 0 (off)
        await SetColorAsync(0, 0, 0, ct);
        _initialized = true;
    }

    public async Task SetColorAsync(byte r, byte g, byte b, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // For common-anode LEDs, invert the PWM values
        byte rVal = CommonAnode ? (byte)(255 - r) : r;
        byte gVal = CommonAnode ? (byte)(255 - g) : g;
        byte bVal = CommonAnode ? (byte)(255 - b) : b;

        await WritePwmAsync(RedPin, rVal, ct);
        await WritePwmAsync(GreenPin, gVal, ct);
        await WritePwmAsync(BluePin, bVal, ct);
    }

    public async Task OffAsync(CancellationToken ct = default)
    {
        await SetColorAsync(0, 0, 0, ct);
    }

    public async Task FadeToAsync(byte r, byte g, byte b, TimeSpan duration, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        const int steps = 50;
        var stepDelay = duration / steps;

        // Read current PWM values (approximate from last write — we track them)
        // For simplicity, we fade from black to target in 'steps' increments
        for (int i = 1; i <= steps; i++)
        {
            ct.ThrowIfCancellationRequested();
            byte cr = (byte)(r * i / steps);
            byte cg = (byte)(g * i / steps);
            byte cb = (byte)(b * i / steps);
            await SetColorAsync(cr, cg, cb, ct);
            await Task.Delay(stepDelay, ct);
        }
    }

    private async Task WritePwmAsync(int pin, byte duty, CancellationToken ct)
    {
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PWM_WRITE, pin, duty, 5000), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"RGB LED PWM write pin {pin} failed: {error}");
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
