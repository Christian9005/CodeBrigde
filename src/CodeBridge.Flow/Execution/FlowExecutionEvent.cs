namespace CodeBridge.Flow.Execution;

public sealed record FlowExecutionEvent(
    FlowExecutionEventKind Kind,
    string NodeId,
    string BlockType,
    IReadOnlyDictionary<string, object?>? Outputs = null,
    string? Message = null);

