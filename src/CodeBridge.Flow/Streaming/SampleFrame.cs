namespace CodeBridge.Flow.Streaming;

/// <summary>
/// One timestamped value emitted by a board acquisition channel.
/// </summary>
public sealed record SampleFrame(
    string ChannelId,
    long Sequence,
    DateTimeOffset Timestamp,
    double Value);
