using CodeBridge.Flow;

namespace CodeBridge.Flow.Execution;

public sealed class FlowExecutionResult
{
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> NodeOutputs { get; }

    public FlowExecutionResult(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> nodeOutputs)
    {
        NodeOutputs = nodeOutputs;
    }

    public T GetOutput<T>(string nodeId, string portName)
    {
        if (!NodeOutputs.TryGetValue(nodeId, out var outputs))
            throw new KeyNotFoundException($"Node '{nodeId}' did not produce outputs.");

        if (!outputs.TryGetValue(portName, out var value))
            throw new KeyNotFoundException($"Node '{nodeId}' did not produce output '{portName}'.");

        return FlowValueConverter.ConvertTo<T>(value, portName);
    }
}
