using CodeBridge.Core.Abstractions;
using CodeBridge.Flow;

namespace CodeBridge.Flow.Execution;

public sealed class FlowNodeExecutionContext
{
    public FlowDocument Document { get; }
    public FlowNode Node { get; }
    public FlowBlockDefinition Definition { get; }
    public IBoard? Board { get; }
    public IReadOnlyDictionary<string, object?> Inputs { get; }
    public CancellationToken CancellationToken { get; }

    public FlowNodeExecutionContext(
        FlowDocument document,
        FlowNode node,
        FlowBlockDefinition definition,
        IBoard? board,
        IReadOnlyDictionary<string, object?> inputs,
        CancellationToken cancellationToken)
    {
        Document = document;
        Node = node;
        Definition = definition;
        Board = board;
        Inputs = inputs;
        CancellationToken = cancellationToken;
    }

    public T GetInput<T>(string name)
    {
        if (!Inputs.TryGetValue(name, out var value))
            throw new InvalidOperationException($"Input '{name}' is required for node '{Node.Id}'.");

        return FlowValueConverter.ConvertTo<T>(value, name);
    }

    public T GetParameter<T>(string name)
    {
        if (Node.Parameters.TryGetValue(name, out var value))
            return FlowValueConverter.ConvertTo<T>(value, name);

        var property = Definition.FindProperty(name);
        if (property?.DefaultValue is not null)
            return FlowValueConverter.ConvertTo<T>(property.DefaultValue, name);

        throw new InvalidOperationException($"Parameter '{name}' is required for node '{Node.Id}'.");
    }

    public T GetInputOrParameter<T>(string inputName, string parameterName)
    {
        if (Inputs.TryGetValue(inputName, out var inputValue))
            return FlowValueConverter.ConvertTo<T>(inputValue, inputName);

        return GetParameter<T>(parameterName);
    }
}
