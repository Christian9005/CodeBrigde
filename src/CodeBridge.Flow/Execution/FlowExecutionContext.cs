using CodeBridge.Core.Abstractions;

namespace CodeBridge.Flow.Execution;

/// <summary>
/// Runtime services available while executing a visual flow.
/// </summary>
public sealed class FlowExecutionContext
{
    public IBoard? Board { get; init; }
    public FlowExecutionEventHandler? EventHandler { get; init; }
    public TimeSpan TraceDelay { get; init; } = TimeSpan.Zero;
    public Dictionary<string, object?> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);
}
