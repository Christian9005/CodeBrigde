using CodeBridge.Flow;

namespace CodeBridge.Flow.Execution;

public delegate ValueTask<IReadOnlyDictionary<string, object?>> FlowBlockHandler(
    FlowNodeExecutionContext context);
