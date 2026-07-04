using System.Runtime.CompilerServices;

namespace CodeBridge.Core.Abstractions.Acquisition;

/// <summary>
/// Owns high-rate board acquisition channels with bounded firmware and host buffers.
/// </summary>
public interface IBoardAcquisitionController : IDisposable
{
    Task<BoardSampleChannel> StartSamplingAsync(
        BoardSampleChannelRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<BoardSampleFrame>> ReadSamplesAsync(
        string channelId,
        int maxFrames,
        CancellationToken ct = default);

    Task StopSamplingAsync(string channelId, CancellationToken ct = default);

    async IAsyncEnumerable<BoardSampleFrame> StreamSamplesAsync(
        string channelId,
        TimeSpan pollInterval,
        int maxFramesPerPoll,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var sample in await ReadSamplesAsync(channelId, maxFramesPerPoll, ct))
                yield return sample;

            await Task.Delay(pollInterval, ct);
        }
    }
}

/// <summary>
/// Optional board capability for firmware-buffered acquisition.
/// </summary>
public interface IBoardWithAcquisition
{
    IBoardAcquisitionController Acquisition { get; }
}
