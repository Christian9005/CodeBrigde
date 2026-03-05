using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32-specific GPIO controller implementation.
/// Sends GPIO commands to the firmware via the transport layer.
/// </summary>
internal class ESP32GpioController : IGpioController
{
    private readonly ITransport _transport;

    public ESP32GpioController(ITransport transport)
    {
        _transport = transport;
    }

    public async Task SetPinModeAsync(int pin, PinMode mode, CancellationToken ct = default)
    {
        ValidatePin(pin);

        var modeValue = mode switch
        {
            PinMode.Input => 0,
            PinMode.Output => 1,
            PinMode.InputPullUp => 2,
            PinMode.InputPullDown => 3,
            PinMode.Analog => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PIN_MODE, pin, modeValue), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Failed to set pin mode: {data}");
    }

    public async Task DigitalWriteAsync(int pin, PinValue value, CancellationToken ct = default)
    {
        ValidatePin(pin);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DIGITAL_WRITE, pin, (int)value), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Failed to write to pin {pin}: {data}");
    }

    public async Task<PinValue> DigitalReadAsync(int pin, CancellationToken ct = default)
    {
        ValidatePin(pin);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DIGITAL_READ, pin), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Failed to read pin {pin}: {data}");

        return data == "1" ? PinValue.High : PinValue.Low;
    }

    public async Task<int> AnalogReadAsync(int pin, CancellationToken ct = default)
    {
        ValidatePin(pin);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_ANALOG_READ, pin), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Failed to analog read pin {pin}: {data}");

        return int.Parse(data);
    }

    public async Task PwmWriteAsync(int pin, int dutyCycle, int frequency = 5000, CancellationToken ct = default)
    {
        ValidatePin(pin);

        if (dutyCycle < 0 || dutyCycle > 255)
            throw new ArgumentOutOfRangeException(nameof(dutyCycle), "Must be 0-255");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PWM_WRITE, pin, dutyCycle, frequency), ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Failed to write PWM to pin {pin}: {data}");
    }

    private static void ValidatePin(int pin)
    {
        // ESP32 valid GPIO: 0-39 (some are input only)
        if (pin < 0 || pin > 39)
            throw new ArgumentOutOfRangeException(nameof(pin), "ESP32 GPIO must be 0-39");
    }

    public void Dispose()
    {
        // Transport is owned by the board, not us
        GC.SuppressFinalize(this);
    }
}
