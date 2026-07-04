namespace CodeBridge.Flow.Streaming;

/// <summary>
/// Defines what happens when a stream produces faster than the consumer can drain.
/// </summary>
public enum BackpressurePolicy
{
    DropOldest,
    DropNewest,
    Aggregate,
    Pause
}
