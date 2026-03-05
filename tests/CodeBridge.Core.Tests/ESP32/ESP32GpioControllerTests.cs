using CodeBridge.Core.Enums;
using CodeBridge.Core.Tests.Mocks;
using CodeBridge.ESP32;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32GpioControllerTests
{
    private readonly MockTransport _transport;
    private readonly ESP32Board _board;

    public ESP32GpioControllerTests()
    {
        _transport = new MockTransport();
        // Queue PING + VER responses for ConnectAsync
        _transport.EnqueueResponses("OK:PONG", "OK:0.1.0");
        _board = new ESP32Board(_transport);
        _board.ConnectAsync().GetAwaiter().GetResult();
    }

    // ── SetPinMode Tests ────────────────────────────────────

    [Theory]
    [InlineData(PinMode.Input, 0)]
    [InlineData(PinMode.Output, 1)]
    [InlineData(PinMode.InputPullUp, 2)]
    [InlineData(PinMode.InputPullDown, 3)]
    [InlineData(PinMode.Analog, 4)]
    public async Task SetPinModeAsync_AllModes_SendsCorrectCommand(PinMode mode, int expectedModeValue)
    {
        _transport.EnqueueResponse("OK");

        await _board.Gpio.SetPinModeAsync(2, mode);

        Assert.Equal($"PM:2:{expectedModeValue}\n", _transport.LastCommand);
    }

    [Fact]
    public async Task SetPinModeAsync_ErrorResponse_ThrowsException()
    {
        _transport.EnqueueResponse("ERR:Invalid mode");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _board.Gpio.SetPinModeAsync(2, PinMode.Output));
    }

    // ── DigitalWrite Tests ──────────────────────────────────

    [Fact]
    public async Task DigitalWriteAsync_High_SendsCorrectCommand()
    {
        _transport.EnqueueResponse("OK");

        await _board.Gpio.DigitalWriteAsync(2, PinValue.High);

        Assert.Equal("DW:2:1\n", _transport.LastCommand);
    }

    [Fact]
    public async Task DigitalWriteAsync_Low_SendsCorrectCommand()
    {
        _transport.EnqueueResponse("OK");

        await _board.Gpio.DigitalWriteAsync(13, PinValue.Low);

        Assert.Equal("DW:13:0\n", _transport.LastCommand);
    }

    [Fact]
    public async Task DigitalWriteAsync_ErrorResponse_ThrowsException()
    {
        _transport.EnqueueResponse("ERR:Failed to write to pin 2");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _board.Gpio.DigitalWriteAsync(2, PinValue.High));
    }

    // ── DigitalRead Tests ───────────────────────────────────

    [Fact]
    public async Task DigitalReadAsync_High_ReturnsHigh()
    {
        _transport.EnqueueResponse("OK:1");

        var result = await _board.Gpio.DigitalReadAsync(4);

        Assert.Equal(PinValue.High, result);
        Assert.Equal("DR:4\n", _transport.LastCommand);
    }

    [Fact]
    public async Task DigitalReadAsync_Low_ReturnsLow()
    {
        _transport.EnqueueResponse("OK:0");

        var result = await _board.Gpio.DigitalReadAsync(4);

        Assert.Equal(PinValue.Low, result);
    }

    // ── AnalogRead Tests ────────────────────────────────────

    [Fact]
    public async Task AnalogReadAsync_ReturnsValue()
    {
        _transport.EnqueueResponse("OK:2048");

        var result = await _board.Gpio.AnalogReadAsync(34);

        Assert.Equal(2048, result);
        Assert.Equal("AR:34\n", _transport.LastCommand);
    }

    [Fact]
    public async Task AnalogReadAsync_ZeroValue_ReturnsZero()
    {
        _transport.EnqueueResponse("OK:0");

        var result = await _board.Gpio.AnalogReadAsync(34);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task AnalogReadAsync_MaxValue_Returns4095()
    {
        _transport.EnqueueResponse("OK:4095");

        var result = await _board.Gpio.AnalogReadAsync(34);

        Assert.Equal(4095, result);
    }

    // ── PwmWrite Tests ──────────────────────────────────────

    [Fact]
    public async Task PwmWriteAsync_DefaultFreq_SendsCorrectCommand()
    {
        _transport.EnqueueResponse("OK");

        await _board.Gpio.PwmWriteAsync(5, 128);

        Assert.Equal("PW:5:128:5000\n", _transport.LastCommand);
    }

    [Fact]
    public async Task PwmWriteAsync_CustomFreq_SendsCorrectCommand()
    {
        _transport.EnqueueResponse("OK");

        await _board.Gpio.PwmWriteAsync(5, 255, 1000);

        Assert.Equal("PW:5:255:1000\n", _transport.LastCommand);
    }

    [Fact]
    public async Task PwmWriteAsync_InvalidDutyCycleTooHigh_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _board.Gpio.PwmWriteAsync(5, 256));
    }

    [Fact]
    public async Task PwmWriteAsync_InvalidDutyCycleNegative_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _board.Gpio.PwmWriteAsync(5, -1));
    }

    // ── Pin Validation Tests ────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(40)]
    [InlineData(100)]
    public async Task SetPinModeAsync_InvalidPin_ThrowsArgumentException(int pin)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _board.Gpio.SetPinModeAsync(pin, PinMode.Output));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(39)]
    public async Task SetPinModeAsync_ValidPin_DoesNotThrow(int pin)
    {
        _transport.EnqueueResponse("OK");
        await _board.Gpio.SetPinModeAsync(pin, PinMode.Output);
        // No exception = pass
    }
}
