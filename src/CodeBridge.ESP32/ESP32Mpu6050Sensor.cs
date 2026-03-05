using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Sensors;
using CodeBridge.Core.Protocol;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// MPU6050 I2C 6-axis IMU — 3-axis accelerometer + 3-axis gyroscope.
/// Default I2C address: 0x68 (alternative: 0x69 with AD0 pin HIGH).
/// </summary>
public class ESP32Mpu6050Sensor : IImuSensor
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    public int Address { get; }
    public string Name => $"MPU6050 (0x{Address:X2})";
    public string Unit => "m/s²";
    public bool IsReady => _initialized;
    public bool HasMagnetometer => false;

    public ESP32Mpu6050Sensor(ITransport transport, int address = 0x68)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Address = address;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MPU6050_INIT, Address), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"MPU6050 init failed: {error}");
        _initialized = true;
    }

    /// <summary>Reads all 6 axes + chip temperature in one I2C burst read.</summary>
    private async Task<(Vector3 Accel, Vector3 Gyro, double TempC)> ReadRawAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MPU6050_READ), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"MPU6050 read failed: {data}");

        // Format: "ax,ay,az,gx,gy,gz,temp"
        var parts = data.Split(',');
        if (parts.Length < 7)
            throw new InvalidOperationException($"Invalid MPU6050 response: {data}");

        var ax = double.Parse(parts[0], CultureInfo.InvariantCulture);
        var ay = double.Parse(parts[1], CultureInfo.InvariantCulture);
        var az = double.Parse(parts[2], CultureInfo.InvariantCulture);
        var gx = double.Parse(parts[3], CultureInfo.InvariantCulture);
        var gy = double.Parse(parts[4], CultureInfo.InvariantCulture);
        var gz = double.Parse(parts[5], CultureInfo.InvariantCulture);
        var temp = double.Parse(parts[6], CultureInfo.InvariantCulture);

        return (new Vector3(ax, ay, az), new Vector3(gx, gy, gz), temp);
    }

    public async Task<ImuReading> ReadAsync(CancellationToken ct = default)
    {
        var (accel, gyro, _) = await ReadRawAsync(ct);
        return new ImuReading(accel, gyro);
    }

    public async Task<Vector3> ReadAccelerationAsync(CancellationToken ct = default)
    {
        var (accel, _, _) = await ReadRawAsync(ct);
        return accel;
    }

    public async Task<Vector3> ReadGyroscopeAsync(CancellationToken ct = default)
    {
        var (_, gyro, _) = await ReadRawAsync(ct);
        return gyro;
    }

    public Task<Vector3> ReadMagnetometerAsync(CancellationToken ct = default)
        => throw new NotSupportedException("MPU6050 does not have a magnetometer. Use MPU9250 instead.");

    public async IAsyncEnumerable<ImuReading> ReadContinuousAsync(
        TimeSpan interval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return await ReadAsync(ct);
            await Task.Delay(interval, ct);
        }
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
