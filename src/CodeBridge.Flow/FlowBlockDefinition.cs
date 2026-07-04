namespace CodeBridge.Flow;

/// <summary>
/// Metadata for a block type that can appear in the visual editor toolbox.
/// </summary>
public sealed class FlowBlockDefinition
{
    public required string Type { get; init; }
    public required string DisplayName { get; init; }
    public string Category { get; init; } = "General";
    public string? Description { get; init; }
    public IReadOnlyList<FlowPortDefinition> Ports { get; init; } = [];
    public IReadOnlyList<FlowPropertyDefinition> Properties { get; init; } = [];

    public IEnumerable<FlowPortDefinition> InputPorts =>
        Ports.Where(port => port.Direction == FlowPortDirection.Input);

    public IEnumerable<FlowPortDefinition> OutputPorts =>
        Ports.Where(port => port.Direction == FlowPortDirection.Output);

    public FlowPortDefinition? FindInput(string name) =>
        InputPorts.FirstOrDefault(port => string.Equals(port.Name, name, StringComparison.OrdinalIgnoreCase));

    public FlowPortDefinition? FindOutput(string name) =>
        OutputPorts.FirstOrDefault(port => string.Equals(port.Name, name, StringComparison.OrdinalIgnoreCase));

    public FlowPropertyDefinition? FindProperty(string name) =>
        Properties.FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));
}

