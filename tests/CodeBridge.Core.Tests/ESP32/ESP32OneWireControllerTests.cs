using CodeBridge.Core.Protocol;
using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

/// <summary>
/// Tests for the ESP32 OneWire controller.
/// </summary>
public class ESP32OneWireControllerTests
{
    private MockTransport CreateTransport(string response = "OK")
    {
        var transport = new MockTransport();
        transport.EnqueueResponse(response);
        return transport;
    }

    // ── Scan ─────────────────────────────────────────────────

    [Fact]
    public async Task ScanAsync_Sends_OWS_Command_With_Pin()
    {
        var transport = CreateTransport("OK:28FF12345678AB,28FF87654321CD");
        var ow = new ESP32OneWireController(transport);

        var addrs = await ow.ScanAsync(4);

        Assert.StartsWith("OWS:", transport.LastCommand);
        Assert.Equal(2, addrs.Length);
        Assert.Equal("28FF12345678AB", addrs[0]);
        Assert.Equal("28FF87654321CD", addrs[1]);
    }

    [Fact]
    public async Task ScanAsync_Returns_Empty_When_No_Devices()
    {
        var transport = CreateTransport("OK:");
        var ow = new ESP32OneWireController(transport);

        var addrs = await ow.ScanAsync(4);

        Assert.Empty(addrs);
    }

    // ── Read ─────────────────────────────────────────────────

    [Fact]
    public async Task ReadAsync_Sends_OWR_Command_And_Returns_Bytes()
    {
        var transport = CreateTransport("OK:AABBCCDD");
        var ow = new ESP32OneWireController(transport);

        var data = await ow.ReadAsync(4, "28FF12345678AB", 4);

        Assert.StartsWith("OWR:", transport.LastCommand);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, data);
    }

    // ── Write ────────────────────────────────────────────────

    [Fact]
    public async Task WriteAsync_Sends_OWW_Command_With_HexData()
    {
        var transport = CreateTransport("OK");
        var ow = new ESP32OneWireController(transport);

        await ow.WriteAsync(4, "28FF12345678AB", new byte[] { 0x44 });

        Assert.StartsWith("OWW:", transport.LastCommand);
        Assert.Contains("44", transport.LastCommand);
    }

    // ── Temperature ──────────────────────────────────────────

    [Fact]
    public async Task ReadTemperatureAsync_Without_Address_Returns_Celsius()
    {
        var transport = CreateTransport("OK:22.50");
        var ow = new ESP32OneWireController(transport);

        var temp = await ow.ReadTemperatureAsync(4);

        Assert.StartsWith("OWT:", transport.LastCommand);
        Assert.Equal(22.5, temp);
    }

    [Fact]
    public async Task ReadTemperatureAsync_With_Address_Sends_Address()
    {
        var transport = CreateTransport("OK:25.75");
        var ow = new ESP32OneWireController(transport);

        var temp = await ow.ReadTemperatureAsync(4, "28FF12345678ABCD");

        Assert.Contains("28FF12345678ABCD", transport.LastCommand);
        Assert.Equal(25.75, temp);
    }

    [Fact]
    public async Task ReadTemperatureAsync_Throws_On_Sensor_Error()
    {
        var transport = CreateTransport("ERR:Sensor not found");
        var ow = new ESP32OneWireController(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ow.ReadTemperatureAsync(4));
    }

    // ── Dispose ──────────────────────────────────────────────

    [Fact]
    public async Task Disposed_Throws_ObjectDisposedException()
    {
        var transport = CreateTransport("OK");
        var ow = new ESP32OneWireController(transport);
        ow.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => ow.ScanAsync(4));
    }
}
