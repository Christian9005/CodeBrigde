using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32Bme280SensorTests
{
    private MockTransport CreateTransport(string response = "OK:23.50,65.00,1013.25")
    {
        var transport = new MockTransport();
        transport.EnqueueResponse(response);
        return transport;
    }

    // ── Init ─────────────────────────────────────────────────

    [Fact]
    public async Task InitAsync_Sends_BMI_Command_Default_Address()
    {
        var transport = CreateTransport("OK");
        var bme = new ESP32Bme280Sensor(transport);

        await bme.InitAsync();

        Assert.Contains("BMI:118", transport.LastCommand); // 0x76 = 118
        Assert.True(bme.IsReady);
    }

    [Fact]
    public async Task InitAsync_Sends_BMI_Command_Custom_Address()
    {
        var transport = CreateTransport("OK");
        var bme = new ESP32Bme280Sensor(transport, 0x77);

        await bme.InitAsync();

        Assert.Contains("BMI:119", transport.LastCommand); // 0x77 = 119
    }

    // ── ReadAll ──────────────────────────────────────────────

    [Fact]
    public async Task ReadAllAsync_Parses_EnvironmentReading()
    {
        var transport = CreateTransport("OK:23.50,65.00,1013.25");
        var bme = new ESP32Bme280Sensor(transport);

        var reading = await bme.ReadAllAsync();

        Assert.Equal(23.50, reading.TemperatureCelsius);
        Assert.Equal(65.00, reading.HumidityPercent);
        Assert.Equal(1013.25, reading.PressureHpa);
        Assert.StartsWith("BMR", transport.LastCommand);
    }

    // ── Temperature ──────────────────────────────────────────

    [Fact]
    public async Task ReadTemperatureCelsiusAsync_Returns_Temp()
    {
        var transport = CreateTransport("OK:25.30,50.00,1010.00");
        var bme = new ESP32Bme280Sensor(transport);

        var temp = await bme.ReadTemperatureCelsiusAsync();

        Assert.Equal(25.30, temp);
    }

    [Fact]
    public async Task ReadTemperatureFahrenheitAsync_Converts_Correctly()
    {
        var transport = CreateTransport("OK:0.00,50.00,1013.25");
        var bme = new ESP32Bme280Sensor(transport);

        var tempF = await bme.ReadTemperatureFahrenheitAsync();

        Assert.Equal(32.0, tempF, 1);
    }

    // ── Humidity ──────────────────────────────────────────────

    [Fact]
    public async Task ReadHumidityAsync_Returns_Humidity()
    {
        var transport = CreateTransport("OK:22.00,78.50,1015.00");
        var bme = new ESP32Bme280Sensor(transport);

        var hum = await bme.ReadHumidityAsync();

        Assert.Equal(78.50, hum);
    }

    // ── Pressure ─────────────────────────────────────────────

    [Fact]
    public async Task ReadPressureHpaAsync_Returns_Pressure()
    {
        var transport = CreateTransport("OK:22.00,60.00,1023.45");
        var bme = new ESP32Bme280Sensor(transport);

        var pres = await bme.ReadPressureHpaAsync();

        Assert.Equal(1023.45, pres);
    }

    [Fact]
    public async Task ReadAltitudeMetersAsync_Calculates_Altitude()
    {
        var transport = CreateTransport("OK:22.00,60.00,1013.25");
        var bme = new ESP32Bme280Sensor(transport);

        var alt = await bme.ReadAltitudeMetersAsync(1013.25);

        Assert.Equal(0.0, alt, 1); // Same as sea level → ~0m
    }

    // ── Properties ───────────────────────────────────────────

    [Fact]
    public void Properties_Return_Correct_Values()
    {
        var transport = new MockTransport();
        var bme = new ESP32Bme280Sensor(transport, 0x77);

        Assert.Equal(0x77, bme.Address);
        Assert.Contains("BME280", bme.Name);
        Assert.Equal("°C", bme.Unit);
        Assert.False(bme.IsReady);
    }

    // ── Error Handling ───────────────────────────────────────

    [Fact]
    public async Task ReadAllAsync_Throws_On_Error_Response()
    {
        var transport = CreateTransport("ERR:Sensor not found");
        var bme = new ESP32Bme280Sensor(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bme.ReadAllAsync());
    }

    // ── Dispose ──────────────────────────────────────────────

    [Fact]
    public async Task Dispose_Prevents_Further_Operations()
    {
        var transport = new MockTransport();
        var bme = new ESP32Bme280Sensor(transport);
        bme.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => bme.InitAsync());
    }
}
