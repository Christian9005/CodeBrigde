namespace CodeBridge.Flow;

/// <summary>
/// Captures the runtime limits the designer should respect for a board family.
/// </summary>
public sealed record BoardRuntimeCapabilities(
    int MaxDigitalSampleRateHz,
    int MaxAnalogSampleRateHz,
    int MaxStreamChannels,
    int MaxBufferCapacity,
    bool SupportsInterrupts,
    bool SupportsHardwareTimers,
    int MinimumTimerIntervalMicroseconds)
{
    public static BoardRuntimeCapabilities Default { get; } = new(
        MaxDigitalSampleRateHz: 1000,
        MaxAnalogSampleRateHz: 500,
        MaxStreamChannels: 4,
        MaxBufferCapacity: 4096,
        SupportsInterrupts: false,
        SupportsHardwareTimers: false,
        MinimumTimerIntervalMicroseconds: 1000);
}
