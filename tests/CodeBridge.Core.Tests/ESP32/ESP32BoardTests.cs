using CodeBridge.Core.Tests.Mocks;
using CodeBridge.ESP32;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32BoardTests
{
    // ── Connection Tests ────────────────────────────────────

    [Fact]
    public async Task ConnectAsync_SuccessfulPing_Connects()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.1.0");

        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        Assert.True(board.IsConnected);
        Assert.Equal("0.1.0", board.FirmwareVersion);
    }

    [Fact]
    public async Task ConnectAsync_PingFails_ThrowsException()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("ERR:Unknown");

        var board = new ESP32Board(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => board.ConnectAsync());
    }

    [Fact]
    public async Task ConnectAsync_WrongPingResponse_ThrowsException()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:NOT_PONG");

        var board = new ESP32Board(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => board.ConnectAsync());
    }

    // ── GetInfo Tests ───────────────────────────────────────

    [Fact]
    public async Task GetInfoAsync_ReturnsCorrectBoardInfo()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.1.0");
        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        var json = """{"chip":"ESP32-D0WD-V3","freq":240,"heap":320000,"flash":4194304,"sdk":"v4.4.6"}""";
        transport.EnqueueResponse($"OK:{json}");

        var info = await board.GetInfoAsync();

        Assert.Equal("ESP32-D0WD-V3", info.ChipModel);
        Assert.Equal(240, info.CpuFrequencyMHz);
        Assert.Equal(320000, info.FreeHeapBytes);
        Assert.Equal(4194304, info.FlashSizeBytes);
        Assert.Equal("v4.4.6", info.SdkVersion);
    }

    [Fact]
    public async Task GetInfoAsync_ErrorResponse_ThrowsException()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.1.0");
        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        transport.EnqueueResponse("ERR:timeout");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => board.GetInfoAsync());
    }

    // ── Properties Tests ────────────────────────────────────

    [Fact]
    public async Task Family_IsESP32()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.1.0");
        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        Assert.Equal(Core.Enums.BoardFamily.ESP32, board.Family);
    }

    [Fact]
    public void Gpio_BeforeConnect_ThrowsException()
    {
        var transport = new MockTransport();
        var board = new ESP32Board(transport);

        Assert.Throws<InvalidOperationException>(() => board.Gpio);
    }

    [Fact]
    public void I2C_BeforeConnect_ThrowsException()
    {
        var transport = new MockTransport();
        var board = new ESP32Board(transport);

        Assert.Throws<InvalidOperationException>(() => board.I2C);
    }

    [Fact]
    public async Task Gpio_AfterConnect_DoesNotThrow()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.1.0");
        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        var gpio = board.Gpio; // Should not throw
        Assert.NotNull(gpio);
    }

    [Fact]
    public async Task Acquisition_AfterConnect_DoesNotThrow()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.1.0");
        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        var acquisition = board.Acquisition;

        Assert.NotNull(acquisition);
    }

    // ── Disconnect Tests ────────────────────────────────────

    [Fact]
    public async Task DisconnectAsync_DisconnectsTransport()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.1.0");
        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        await board.DisconnectAsync();

        Assert.False(transport.IsConnected);
    }

    // ── Reset Tests ─────────────────────────────────────────

    [Fact]
    public async Task ResetAsync_SendsResetCommand()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:0.1.0");
        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        transport.EnqueueResponse("OK:Resetting...");
        await board.ResetAsync();

        Assert.Equal("RST\n", transport.LastCommand);
    }

    // ── FirmwareVersion Tests ───────────────────────────────

    [Fact]
    public async Task FirmwareVersion_AfterConnect_ReturnsVersion()
    {
        var transport = new MockTransport();
        transport.EnqueueResponses("OK:PONG", "OK:1.2.3");
        var board = new ESP32Board(transport);
        await board.ConnectAsync();

        Assert.Equal("1.2.3", board.FirmwareVersion);
    }

    [Fact]
    public void FirmwareVersion_BeforeConnect_ReturnsUnknown()
    {
        var transport = new MockTransport();
        var board = new ESP32Board(transport);

        Assert.Equal("unknown", board.FirmwareVersion);
    }
}
