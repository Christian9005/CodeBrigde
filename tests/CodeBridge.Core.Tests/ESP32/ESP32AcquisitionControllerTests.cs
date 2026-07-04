using CodeBridge.Core.Abstractions.Acquisition;
using CodeBridge.Core.Tests.Mocks;
using CodeBridge.ESP32;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32AcquisitionControllerTests
{
    [Fact]
    public async Task StartSamplingAsync_Sends_SCFG_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:S0");
        var acquisition = new ESP32AcquisitionController(transport);

        var channel = await acquisition.StartSamplingAsync(new BoardSampleChannelRequest(
            Pin: 32,
            IsAnalog: true,
            Mode: BoardSamplingMode.HardwareTimer,
            SampleRateHz: 1000,
            BufferCapacity: 4096,
            Backpressure: BoardBackpressurePolicy.DropOldest,
            BatchSize: 64));

        Assert.Equal("S0", channel.Id);
        Assert.StartsWith("SCFG:32:1:1:1000:4096:0:64", transport.LastCommand);
    }

    [Fact]
    public async Task ReadSamplesAsync_Parses_Frames()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:0,1000,42;1,2000,43");
        var acquisition = new ESP32AcquisitionController(transport);

        var frames = await acquisition.ReadSamplesAsync("S0", 32);

        Assert.Equal(2, frames.Count);
        Assert.Equal("S0", frames[0].ChannelId);
        Assert.Equal(0, frames[0].Sequence);
        Assert.Equal(TimeSpan.FromMicroseconds(1000), frames[0].Elapsed);
        Assert.Equal(42, frames[0].Value);
        Assert.StartsWith("SRD:S0:32", transport.LastCommand);
    }

    [Fact]
    public async Task ReadSamplesAsync_Returns_Empty_When_None()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:NONE");
        var acquisition = new ESP32AcquisitionController(transport);

        var frames = await acquisition.ReadSamplesAsync("S0", 32);

        Assert.Empty(frames);
    }

    [Fact]
    public async Task StopSamplingAsync_Sends_SSTOP_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK");
        var acquisition = new ESP32AcquisitionController(transport);

        await acquisition.StopSamplingAsync("S0");

        Assert.StartsWith("SSTOP:S0", transport.LastCommand);
    }
}
