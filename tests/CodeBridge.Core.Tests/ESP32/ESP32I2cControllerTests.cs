using CodeBridge.Core.Tests.Mocks;
using CodeBridge.ESP32;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32I2cControllerTests
{
    private readonly MockTransport _transport;
    private readonly ESP32Board _board;

    public ESP32I2cControllerTests()
    {
        _transport = new MockTransport();
        _transport.EnqueueResponses("OK:PONG", "OK:0.1.0");
        _board = new ESP32Board(_transport);
        _board.ConnectAsync().GetAwaiter().GetResult();
    }

    // ── Scan Tests ──────────────────────────────────────────

    [Fact]
    public async Task ScanAsync_DevicesFound_ReturnsAddresses()
    {
        _transport.EnqueueResponse("OK:60,104,112");

        var devices = await _board.I2C.ScanAsync();

        Assert.Equal(3, devices.Count);
        Assert.Equal(60, devices[0]);   // 0x3C - OLED display
        Assert.Equal(104, devices[1]);  // 0x68 - MPU6050
        Assert.Equal(112, devices[2]);  // 0x70
        Assert.Equal("IS\n", _transport.LastCommand);
    }

    [Fact]
    public async Task ScanAsync_NoDevices_ReturnsEmptyList()
    {
        _transport.EnqueueResponse("OK:");

        var devices = await _board.I2C.ScanAsync();

        Assert.Empty(devices);
    }

    [Fact]
    public async Task ScanAsync_SingleDevice_ReturnsOneAddress()
    {
        _transport.EnqueueResponse("OK:60");

        var devices = await _board.I2C.ScanAsync();

        Assert.Single(devices);
        Assert.Equal(60, devices[0]);
    }

    [Fact]
    public async Task ScanAsync_ErrorResponse_ThrowsException()
    {
        _transport.EnqueueResponse("ERR:I2C error");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _board.I2C.ScanAsync());
    }

    // ── Write Tests ─────────────────────────────────────────

    [Fact]
    public async Task WriteAsync_SendsCorrectHexData()
    {
        _transport.EnqueueResponse("OK");

        await _board.I2C.WriteAsync(0x3C, new byte[] { 0xAE, 0xD5, 0x80 });

        Assert.Equal("IW:60:AED580\n", _transport.LastCommand);
    }

    [Fact]
    public async Task WriteAsync_SingleByte_CorrectFormat()
    {
        _transport.EnqueueResponse("OK");

        await _board.I2C.WriteAsync(0x68, new byte[] { 0xFF });

        Assert.Equal("IW:104:FF\n", _transport.LastCommand);
    }

    [Fact]
    public async Task WriteAsync_ErrorResponse_ThrowsException()
    {
        _transport.EnqueueResponse("ERR:I2C write failed");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _board.I2C.WriteAsync(0x3C, new byte[] { 0xAE }));
    }

    // ── Read Tests ──────────────────────────────────────────

    [Fact]
    public async Task ReadAsync_ReturnsCorrectBytes()
    {
        _transport.EnqueueResponse("OK:AED580");

        var data = await _board.I2C.ReadAsync(0x3C, 3);

        Assert.Equal(3, data.Length);
        Assert.Equal(0xAE, data[0]);
        Assert.Equal(0xD5, data[1]);
        Assert.Equal(0x80, data[2]);
        Assert.Equal("IR:60:3\n", _transport.LastCommand);
    }

    [Fact]
    public async Task ReadAsync_SingleByte_ReturnsOneByte()
    {
        _transport.EnqueueResponse("OK:FF");

        var data = await _board.I2C.ReadAsync(0x68, 1);

        Assert.Single(data);
        Assert.Equal(0xFF, data[0]);
    }

    // ── Register Write Tests ────────────────────────────────

    [Fact]
    public async Task WriteRegisterAsync_SendsCorrectCommand()
    {
        _transport.EnqueueResponse("OK");

        await _board.I2C.WriteRegisterAsync(0x68, 0x6B, new byte[] { 0x00 });

        Assert.Equal("IWR:104:107:00\n", _transport.LastCommand);
    }

    [Fact]
    public async Task WriteRegisterAsync_MultiByteData_CorrectFormat()
    {
        _transport.EnqueueResponse("OK");

        await _board.I2C.WriteRegisterAsync(0x3C, 0x40, new byte[] { 0x01, 0x02, 0x03 });

        Assert.Equal("IWR:60:64:010203\n", _transport.LastCommand);
    }

    // ── Register Read Tests ─────────────────────────────────

    [Fact]
    public async Task ReadRegisterAsync_ReturnsCorrectData()
    {
        _transport.EnqueueResponse("OK:1A2B");

        var data = await _board.I2C.ReadRegisterAsync(0x68, 0x3B, 2);

        Assert.Equal(2, data.Length);
        Assert.Equal(0x1A, data[0]);
        Assert.Equal(0x2B, data[1]);
        Assert.Equal("IRR:104:59:2\n", _transport.LastCommand);
    }

    [Fact]
    public async Task ReadRegisterAsync_ErrorResponse_ThrowsException()
    {
        _transport.EnqueueResponse("ERR:I2C register read failed");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _board.I2C.ReadRegisterAsync(0x68, 0x3B, 2));
    }
}
