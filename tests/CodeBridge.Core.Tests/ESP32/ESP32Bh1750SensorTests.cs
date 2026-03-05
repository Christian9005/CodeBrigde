using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32Bh1750SensorTests
{
    private MockTransport CreateTransport(string response = "OK:320.50")
    {
        var transport = new MockTransport();
        transport.EnqueueResponse(response);
        return transport;
    }

    // ── Init ─────────────────────────────────────────────────

    [Fact]
    public async Task InitAsync_Sends_BLI_Command_Default_Address()
    {
        var transport = CreateTransport("OK");
        var sensor = new ESP32Bh1750Sensor(transport);

        await sensor.InitAsync();

        Assert.Contains("BLI:35", transport.LastCommand); // 0x23 = 35
        Assert.True(sensor.IsReady);
    }

    [Fact]
    public async Task InitAsync_Sends_BLI_Command_Custom_Address()
    {
        var transport = CreateTransport("OK");
        var sensor = new ESP32Bh1750Sensor(transport, 0x5C);

        await sensor.InitAsync();

        Assert.Contains("BLI:92", transport.LastCommand); // 0x5C = 92
    }

    // ── ReadLux ──────────────────────────────────────────────

    [Fact]
    public async Task ReadLuxAsync_Returns_Lux_Value()
    {
        var transport = CreateTransport("OK:320.50");
        var sensor = new ESP32Bh1750Sensor(transport);

        var lux = await sensor.ReadLuxAsync();

        Assert.Equal(320.50, lux);
        Assert.StartsWith("BLR", transport.LastCommand);
    }

    [Fact]
    public async Task ReadAsync_Returns_Same_As_ReadLux()
    {
        var transport = CreateTransport("OK:1500.00");
        var sensor = new ESP32Bh1750Sensor(transport);

        var value = await sensor.ReadAsync();

        Assert.Equal(1500.00, value);
    }

    [Fact]
    public async Task ReadLuxAsync_Low_Light()
    {
        var transport = CreateTransport("OK:0.50");
        var sensor = new ESP32Bh1750Sensor(transport);

        var lux = await sensor.ReadLuxAsync();

        Assert.Equal(0.50, lux);
    }

    // ── Properties ───────────────────────────────────────────

    [Fact]
    public void Properties_Return_Correct_Values()
    {
        var transport = new MockTransport();
        var sensor = new ESP32Bh1750Sensor(transport, 0x5C);

        Assert.Equal(0x5C, sensor.Address);
        Assert.Contains("BH1750", sensor.Name);
        Assert.Equal("lux", sensor.Unit);
        Assert.False(sensor.IsReady);
    }

    // ── Error Handling ───────────────────────────────────────

    [Fact]
    public async Task ReadLuxAsync_Throws_On_Error()
    {
        var transport = CreateTransport("ERR:Not ready");
        var sensor = new ESP32Bh1750Sensor(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sensor.ReadLuxAsync());
    }

    // ── Dispose ──────────────────────────────────────────────

    [Fact]
    public async Task Dispose_Prevents_Further_Operations()
    {
        var transport = new MockTransport();
        var sensor = new ESP32Bh1750Sensor(transport);
        sensor.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => sensor.ReadLuxAsync());
    }
}
