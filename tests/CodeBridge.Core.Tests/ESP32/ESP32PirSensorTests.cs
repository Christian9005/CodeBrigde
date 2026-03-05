using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32PirSensorTests
{
    private MockTransport CreateTransport(string response = "OK:1")
    {
        var transport = new MockTransport();
        transport.EnqueueResponse(response);
        return transport;
    }

    // ── Init ─────────────────────────────────────────────────

    [Fact]
    public async Task InitAsync_Sets_PinMode_Input()
    {
        var transport = CreateTransport("OK");
        var pir = new ESP32PirSensor(transport, 13);

        await pir.InitAsync();

        Assert.Contains("PM:13:0", transport.LastCommand); // PinMode.Input = 0
        Assert.True(pir.IsReady);
    }

    // ── Motion Detection ─────────────────────────────────────

    [Fact]
    public async Task IsMotionDetectedAsync_Returns_True_When_High()
    {
        var transport = CreateTransport("OK:1");
        var pir = new ESP32PirSensor(transport, 13);

        var detected = await pir.IsMotionDetectedAsync();

        Assert.True(detected);
        Assert.Contains("DR:13", transport.LastCommand);
    }

    [Fact]
    public async Task IsMotionDetectedAsync_Returns_False_When_Low()
    {
        var transport = CreateTransport("OK:0");
        var pir = new ESP32PirSensor(transport, 13);

        var detected = await pir.IsMotionDetectedAsync();

        Assert.False(detected);
    }

    [Fact]
    public async Task ReadAsync_Returns_Same_As_IsMotionDetected()
    {
        var transport = CreateTransport("OK:1");
        var pir = new ESP32PirSensor(transport, 13);

        var result = await pir.ReadAsync();

        Assert.True(result);
    }

    // ── Properties ───────────────────────────────────────────

    [Fact]
    public void Properties_Return_Correct_Values()
    {
        var pir = new ESP32PirSensor(new MockTransport(), 27);

        Assert.Equal(27, pir.Pin);
        Assert.Contains("PIR", pir.Name);
        Assert.Contains("HC-SR501", pir.Name);
        Assert.Equal("motion", pir.Unit);
        Assert.False(pir.IsReady);
    }

    // ── Error Handling ───────────────────────────────────────

    [Fact]
    public async Task IsMotionDetectedAsync_Throws_On_Error()
    {
        var transport = CreateTransport("ERR:Pin error");
        var pir = new ESP32PirSensor(transport, 13);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pir.IsMotionDetectedAsync());
    }

    // ── Dispose ──────────────────────────────────────────────

    [Fact]
    public async Task Dispose_Prevents_Further_Operations()
    {
        var pir = new ESP32PirSensor(new MockTransport(), 13);
        pir.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => pir.IsMotionDetectedAsync());
    }
}
