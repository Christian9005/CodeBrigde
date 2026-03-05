using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32Mpu6050SensorTests
{
    private const string SAMPLE_RESPONSE = "OK:1.2345,0.5678,-9.8100,0.0123,-0.0456,0.0789,25.50";

    private MockTransport CreateTransport(string response = SAMPLE_RESPONSE)
    {
        var transport = new MockTransport();
        transport.EnqueueResponse(response);
        return transport;
    }

    // ── Init ─────────────────────────────────────────────────

    [Fact]
    public async Task InitAsync_Sends_MPI_Command_Default_Address()
    {
        var transport = CreateTransport("OK");
        var imu = new ESP32Mpu6050Sensor(transport);

        await imu.InitAsync();

        Assert.Contains("MPI:104", transport.LastCommand); // 0x68 = 104
        Assert.True(imu.IsReady);
    }

    [Fact]
    public async Task InitAsync_Sends_MPI_Command_Custom_Address()
    {
        var transport = CreateTransport("OK");
        var imu = new ESP32Mpu6050Sensor(transport, 0x69);

        await imu.InitAsync();

        Assert.Contains("MPI:105", transport.LastCommand); // 0x69 = 105
    }

    // ── ReadAsync (ImuReading) ───────────────────────────────

    [Fact]
    public async Task ReadAsync_Returns_ImuReading()
    {
        var transport = CreateTransport(SAMPLE_RESPONSE);
        var imu = new ESP32Mpu6050Sensor(transport);

        var reading = await imu.ReadAsync();

        Assert.Equal(1.2345, reading.Acceleration.X, 4);
        Assert.Equal(0.5678, reading.Acceleration.Y, 4);
        Assert.Equal(-9.81, reading.Acceleration.Z, 2);
        Assert.Equal(0.0123, reading.Gyroscope.X, 4);
        Assert.Equal(-0.0456, reading.Gyroscope.Y, 4);
        Assert.Equal(0.0789, reading.Gyroscope.Z, 4);
        Assert.StartsWith("MPR", transport.LastCommand);
    }

    // ── ReadAcceleration ─────────────────────────────────────

    [Fact]
    public async Task ReadAccelerationAsync_Returns_Vector3()
    {
        var transport = CreateTransport(SAMPLE_RESPONSE);
        var imu = new ESP32Mpu6050Sensor(transport);

        var accel = await imu.ReadAccelerationAsync();

        Assert.Equal(1.2345, accel.X, 4);
        Assert.Equal(0.5678, accel.Y, 4);
        Assert.Equal(-9.81, accel.Z, 2);
    }

    // ── ReadGyroscope ────────────────────────────────────────

    [Fact]
    public async Task ReadGyroscopeAsync_Returns_Vector3()
    {
        var transport = CreateTransport(SAMPLE_RESPONSE);
        var imu = new ESP32Mpu6050Sensor(transport);

        var gyro = await imu.ReadGyroscopeAsync();

        Assert.Equal(0.0123, gyro.X, 4);
        Assert.Equal(-0.0456, gyro.Y, 4);
        Assert.Equal(0.0789, gyro.Z, 4);
    }

    // ── Magnetometer (not supported) ─────────────────────────

    [Fact]
    public void HasMagnetometer_Returns_False()
    {
        var imu = new ESP32Mpu6050Sensor(new MockTransport());
        Assert.False(imu.HasMagnetometer);
    }

    [Fact]
    public async Task ReadMagnetometerAsync_Throws_NotSupported()
    {
        var imu = new ESP32Mpu6050Sensor(new MockTransport());
        await Assert.ThrowsAsync<NotSupportedException>(() => imu.ReadMagnetometerAsync());
    }

    // ── Properties ───────────────────────────────────────────

    [Fact]
    public void Properties_Return_Correct_Values()
    {
        var imu = new ESP32Mpu6050Sensor(new MockTransport(), 0x69);

        Assert.Equal(0x69, imu.Address);
        Assert.Contains("MPU6050", imu.Name);
        Assert.Equal("m/s²", imu.Unit);
        Assert.False(imu.IsReady);
    }

    // ── Error Handling ───────────────────────────────────────

    [Fact]
    public async Task ReadAsync_Throws_On_Error_Response()
    {
        var transport = CreateTransport("ERR:MPU6050 not initialized");
        var imu = new ESP32Mpu6050Sensor(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(() => imu.ReadAsync());
    }

    // ── Dispose ──────────────────────────────────────────────

    [Fact]
    public async Task Dispose_Prevents_Further_Operations()
    {
        var imu = new ESP32Mpu6050Sensor(new MockTransport());
        imu.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => imu.InitAsync());
    }
}
