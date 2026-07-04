namespace CodeBridge.Flow;

/// <summary>
/// Defines a configurable block property, such as a pin number or threshold.
/// </summary>
public sealed record FlowPropertyDefinition(
    string Name,
    FlowValueKind ValueKind,
    bool Required = false,
    object? DefaultValue = null,
    IReadOnlyList<FlowPropertyOption>? Options = null,
    bool IsAdvanced = false);
