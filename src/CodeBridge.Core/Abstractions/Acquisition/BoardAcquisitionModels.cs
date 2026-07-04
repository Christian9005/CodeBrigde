namespace CodeBridge.Core.Abstractions.Acquisition;

/// <summary>
/// Selects how the board should produce samples for an acquisition channel.
/// </summary>
public enum BoardSamplingMode
{
    Polling = 0,
    HardwareTimer = 1,
    Interrupt = 2
}

/// <summary>
/// Defines the behavior used when the firmware buffer is full.
/// </summary>
public enum BoardBackpressurePolicy
{
    DropOldest = 0,
    DropNewest = 1,
    Aggregate = 2,
    Pause = 3
}

/// <summary>
/// Describes one acquisition channel requested from a board.
/// </summary>
public sealed record BoardSampleChannelRequest(
    int Pin,
    bool IsAnalog,
    BoardSamplingMode Mode,
    int SampleRateHz,
    int BufferCapacity,
    BoardBackpressurePolicy Backpressure,
    int BatchSize = 32);

/// <summary>
/// A firmware-allocated sample channel.
/// </summary>
public sealed record BoardSampleChannel(
    string Id,
    BoardSampleChannelRequest Request);

/// <summary>
/// One timestamped sample returned by the board firmware.
/// </summary>
public sealed record BoardSampleFrame(
    string ChannelId,
    long Sequence,
    TimeSpan Elapsed,
    double Value);
