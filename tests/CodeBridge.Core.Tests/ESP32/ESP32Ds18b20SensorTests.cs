using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32Ds18b20SensorTests
{
    private MockTransport CreateTransport(string response = "OK:22.75")
    {
        var transport = new MockTransport();
        transport.EnqueueResponse(response);
        return transport;
    }

    // ── Init (no-op) ─────────────────────────────────────────

    [Fact]
    public async Task InitAsync_Completes_Without_Sending_Command()
    {
        var transport = new MockTransport();
        var sensor = new ESP32Ds18b20Sensor(transport, 4);

        await sensor.InitAsync();

        Assert.Empty(transport.SentCommands);
        Assert.True(sensor.IsReady);
    }

    // ── ReadTemperature ──────────────────────────────────────

    [Fact]
    public async Task ReadTemperatureCelsiusAsync_Returns_Temperature()
    {
        var transport = CreateTransport("OK:22.75");
        var sensor = new ESP32Ds18b20Sensor(transport, 4);

        var temp = await sensor.ReadTemperatureCelsiusAsync();

        Assert.Equal(22.75, temp);
        Assert.Contains("OWT:4", transport.LastCommand);
    }

    [Fact]
    public async Task ReadTemperatureFahrenheitAsync_Converts_Correctly()
    {
        var transport = CreateTransport("OK:100.00");
        var sensor = new ESP32Ds18b20Sensor(transport, 4);

        var tempF = await sensor.ReadTemperatureFahrenheitAsync();

        Assert.Equal(212.0, tempF, 1);
    }

    [Fact]
    public async Task ReadAsync_Returns_Temperature_In_Celsius()
    {
        var transport = CreateTransport("OK:25.50");
        var sensor = new ESP32Ds18b20Sensor(transport, 4);

        var temp = await sensor.ReadAsync();

        Assert.Equal(25.50, temp);
    }

    // ── Multiple Sensors on Bus ──────────────────────────────

    [Fact]
    public async Task ReadTemperatureCelsiusAsync_Takes_First_From_MultipleSensors()
    {
        var transport = CreateTransport("OK:22.50,23.75,24.00");
        var sensor = new ESP32Ds18b20Sensor(transport, 4);

        var temp = await sensor.ReadTemperatureCelsiusAsync();

        Assert.Equal(22.50, temp); // Takes first sensor
    }

    // ── Properties ───────────────────────────────────────────

    [Fact]
    public void Properties_Return_Correct_Values()
    {
        var sensor = new ESP32Ds18b20Sensor(new MockTransport(), 15);

        Assert.Equal(15, sensor.Pin);
        Assert.Contains("DS18B20", sensor.Name);
        Assert.Equal("°C", sensor.Unit);
    }

    // ── Error Handling ───────────────────────────────────────

    [Fact]
    public async Task ReadTemperatureCelsiusAsync_Throws_On_Error()
    {
        var transport = CreateTransport("ERR:No sensors found");
        var sensor = new ESP32Ds18b20Sensor(transport, 4);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sensor.ReadTemperatureCelsiusAsync());
    }

    // ── Dispose ──────────────────────────────────────────────

    [Fact]
    public async Task Dispose_Prevents_Further_Operations()
    {
        var sensor = new ESP32Ds18b20Sensor(new MockTransport(), 4);
        sensor.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => sensor.ReadTemperatureCelsiusAsync());
    }
}
