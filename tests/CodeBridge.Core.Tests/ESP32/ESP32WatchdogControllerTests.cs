using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32WatchdogControllerTests
{
    // ── Enable ───────────────────────────────────────────────

    [Fact]
    public async Task EnableAsync_Sends_WDI_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK");
        var wdt = new ESP32WatchdogController(transport);

        await wdt.EnableAsync(TimeSpan.FromSeconds(5));

        Assert.True(wdt.IsEnabled);
        Assert.Contains("WDI:5000", transport.LastCommand);
    }

    [Fact]
    public async Task EnableAsync_Sends_Correct_Timeout()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK");
        var wdt = new ESP32WatchdogController(transport);

        await wdt.EnableAsync(TimeSpan.FromSeconds(30));

        Assert.Contains("WDI:30000", transport.LastCommand);
    }

    [Fact]
    public async Task EnableAsync_Throws_For_Too_Short_Timeout()
    {
        var transport = new MockTransport();
        var wdt = new ESP32WatchdogController(transport);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => wdt.EnableAsync(TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task EnableAsync_Throws_For_Too_Long_Timeout()
    {
        var transport = new MockTransport();
        var wdt = new ESP32WatchdogController(transport);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => wdt.EnableAsync(TimeSpan.FromSeconds(121)));
    }

    [Fact]
    public async Task EnableAsync_Throws_On_Error()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("ERR:WDT init failed");
        var wdt = new ESP32WatchdogController(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => wdt.EnableAsync(TimeSpan.FromSeconds(5)));
    }

    // ── Feed ─────────────────────────────────────────────────

    [Fact]
    public async Task FeedAsync_Sends_WDF_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK");
        var wdt = new ESP32WatchdogController(transport);

        await wdt.FeedAsync();

        Assert.StartsWith("WDF", transport.LastCommand);
    }

    [Fact]
    public async Task FeedAsync_Throws_On_Error()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("ERR:WDT not enabled");
        var wdt = new ESP32WatchdogController(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(() => wdt.FeedAsync());
    }

    // ── Disable ──────────────────────────────────────────────

    [Fact]
    public async Task DisableAsync_Sends_WDD_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK");
        transport.EnqueueResponse("OK");
        var wdt = new ESP32WatchdogController(transport);

        await wdt.EnableAsync(TimeSpan.FromSeconds(5));
        await wdt.DisableAsync();

        Assert.False(wdt.IsEnabled);
        Assert.StartsWith("WDD", transport.LastCommand);
    }

    [Fact]
    public async Task DisableAsync_Throws_On_Error()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("ERR:WDT not enabled");
        var wdt = new ESP32WatchdogController(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(() => wdt.DisableAsync());
    }

    // ── Properties ───────────────────────────────────────────

    [Fact]
    public void IsEnabled_False_Initially()
    {
        var transport = new MockTransport();
        var wdt = new ESP32WatchdogController(transport);

        Assert.False(wdt.IsEnabled);
    }
}
