namespace CodeBridge.Flow;

/// <summary>
/// Describes a selectable value for a block property.
/// </summary>
public sealed record FlowPropertyOption(
    string Label,
    object? Value,
    string? Description = null);
