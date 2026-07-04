using CodeBridge.Flow.Streaming;

namespace CodeBridge.Core.Tests.Flow;

public class BoundedRingBufferTests
{
    [Fact]
    public void TryWrite_DropOldest_KeepsNewestSamples()
    {
        var buffer = new BoundedRingBuffer<int>(3);

        Assert.True(buffer.TryWrite(1));
        Assert.True(buffer.TryWrite(2));
        Assert.True(buffer.TryWrite(3));
        Assert.True(buffer.TryWrite(4));

        Assert.Equal(new[] { 2, 3, 4 }, buffer.Snapshot());
        Assert.Equal(1, buffer.DroppedSamples);
    }

    [Fact]
    public void TryWrite_DropNewest_PreservesExistingSamples()
    {
        var buffer = new BoundedRingBuffer<int>(2, BackpressurePolicy.DropNewest);

        Assert.True(buffer.TryWrite(1));
        Assert.True(buffer.TryWrite(2));
        Assert.False(buffer.TryWrite(3));

        Assert.Equal(new[] { 1, 2 }, buffer.Snapshot());
        Assert.Equal(1, buffer.DroppedSamples);
    }
}
