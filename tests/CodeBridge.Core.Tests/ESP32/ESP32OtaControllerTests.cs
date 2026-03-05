using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32OtaControllerTests
{
    // ── UpdateFromUrl ────────────────────────────────────────

    [Fact]
    public async Task UpdateFromUrlAsync_Sends_OTAB_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:OTA_COMPLETE");
        var ota = new ESP32OtaController(transport);

        await ota.UpdateFromUrlAsync("http://server.local/firmware.bin");

        Assert.Contains("OTAB:http://server.local/firmware.bin", transport.LastCommand);
    }

    [Fact]
    public async Task UpdateFromUrlAsync_Throws_On_Empty_Url()
    {
        var transport = new MockTransport();
        var ota = new ESP32OtaController(transport);

        await Assert.ThrowsAsync<ArgumentException>(
            () => ota.UpdateFromUrlAsync(""));
    }

    [Fact]
    public async Task UpdateFromUrlAsync_Throws_On_Null_Url()
    {
        var transport = new MockTransport();
        var ota = new ESP32OtaController(transport);

        await Assert.ThrowsAsync<ArgumentException>(
            () => ota.UpdateFromUrlAsync(null!));
    }

    [Fact]
    public async Task UpdateFromUrlAsync_Throws_On_Http_Error()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("ERR:HTTP 404");
        var ota = new ESP32OtaController(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ota.UpdateFromUrlAsync("http://server.local/missing.bin"));
    }

    // ── GetStatus ────────────────────────────────────────────

    [Fact]
    public async Task GetStatusAsync_Parses_Idle()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:idle,0");
        var ota = new ESP32OtaController(transport);

        var (status, progress) = await ota.GetStatusAsync();

        Assert.Equal("idle", status);
        Assert.Equal(0, progress);
        Assert.StartsWith("OTAS", transport.LastCommand);
    }

    [Fact]
    public async Task GetStatusAsync_Parses_Downloading()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:downloading,45");
        var ota = new ESP32OtaController(transport);

        var (status, progress) = await ota.GetStatusAsync();

        Assert.Equal("downloading", status);
        Assert.Equal(45, progress);
    }

    [Fact]
    public async Task GetStatusAsync_Parses_Complete()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:complete,100");
        var ota = new ESP32OtaController(transport);

        var (status, progress) = await ota.GetStatusAsync();

        Assert.Equal("complete", status);
        Assert.Equal(100, progress);
    }

    [Fact]
    public async Task GetStatusAsync_Throws_On_Error()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("ERR:OTA status failed");
        var ota = new ESP32OtaController(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(() => ota.GetStatusAsync());
    }
}
