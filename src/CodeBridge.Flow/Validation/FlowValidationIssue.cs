namespace CodeBridge.Flow.Validation;

public sealed record FlowValidationIssue(
    FlowValidationSeverity Severity,
    string Message,
    string? NodeId = null,
    string? ConnectionId = null);

