namespace CodeBridge.Flow.Streaming;

/// <summary>
/// Selects how a board produces values for a sampling channel.
/// </summary>
public enum SamplingMode
{
    Polling,
    HardwareTimer,
    Interrupt
}
