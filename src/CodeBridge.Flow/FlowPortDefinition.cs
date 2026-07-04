namespace CodeBridge.Flow;

/// <summary>
/// Defines one connectable input or output on a block type.
/// </summary>
public sealed record FlowPortDefinition(
    string Name,
    FlowPortDirection Direction,
    FlowValueKind ValueKind,
    bool Required = true)
{
    public static FlowPortDefinition Input(string name, FlowValueKind valueKind, bool required = true)
        => new(name, FlowPortDirection.Input, valueKind, required);

    public static FlowPortDefinition Output(string name, FlowValueKind valueKind)
        => new(name, FlowPortDirection.Output, valueKind, Required: false);
}
