using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32PowerControllerTests
{
    // ── DeepSleep (timed) ────────────────────────────────────

    [Fact]
    public async Task DeepSleepAsync_Sends_DSL_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:SLEEPING");
        var power = new ESP32PowerController(transport);

        await power.DeepSleepAsync(TimeSpan.FromSeconds(60));

        Assert.Contains("DSL:60", transport.LastCommand);
    }

    [Fact]
    public async Task DeepSleepAsync_10_Minutes()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:SLEEPING");
        var power = new ESP32PowerController(transport);

        await power.DeepSleepAsync(TimeSpan.FromMinutes(10));

        Assert.Contains("DSL:600", transport.LastCommand);
    }

    [Fact]
    public async Task DeepSleepAsync_Throws_For_Too_Short()
    {
        var transport = new MockTransport();
        var power = new ESP32PowerController(transport);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => power.DeepSleepAsync(TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task DeepSleepAsync_Throws_For_Too_Long()
    {
        var transport = new MockTransport();
        var power = new ESP32PowerController(transport);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => power.DeepSleepAsync(TimeSpan.FromHours(25)));
    }

    [Fact]
    public async Task DeepSleepAsync_Throws_On_Error()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("ERR:FAIL");
        var power = new ESP32PowerController(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => power.DeepSleepAsync(TimeSpan.FromSeconds(10)));
    }

    // ── DeepSleep (pin wake) ─────────────────────────────────

    [Fact]
    public async Task DeepSleepUntilPinAsync_WakeOnHigh()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:SLEEPING");
        var power = new ESP32PowerController(transport);

        await power.DeepSleepUntilPinAsync(33, wakeOnHigh: true);

        Assert.Contains("DSLP:33:1", transport.LastCommand);
    }

    [Fact]
    public async Task DeepSleepUntilPinAsync_WakeOnLow()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:SLEEPING");
        var power = new ESP32PowerController(transport);

        await power.DeepSleepUntilPinAsync(27, wakeOnHigh: false);

        Assert.Contains("DSLP:27:0", transport.LastCommand);
    }

    [Fact]
    public async Task DeepSleepUntilPinAsync_Throws_On_Error()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("ERR:FAIL");
        var power = new ESP32PowerController(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => power.DeepSleepUntilPinAsync(33));
    }
}
