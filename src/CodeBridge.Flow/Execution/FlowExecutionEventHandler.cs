namespace CodeBridge.Flow.Execution;

public delegate ValueTask FlowExecutionEventHandler(
    FlowExecutionEvent executionEvent,
    CancellationToken cancellationToken);

