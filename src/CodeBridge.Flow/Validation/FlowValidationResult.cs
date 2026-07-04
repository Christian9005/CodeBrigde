namespace CodeBridge.Flow.Validation;

public sealed class FlowValidationResult
{
    public IReadOnlyList<FlowValidationIssue> Issues { get; }
    public bool IsValid => Issues.All(issue => issue.Severity != FlowValidationSeverity.Error);
    public IEnumerable<FlowValidationIssue> Errors => Issues.Where(issue => issue.Severity == FlowValidationSeverity.Error);
    public IEnumerable<FlowValidationIssue> Warnings => Issues.Where(issue => issue.Severity == FlowValidationSeverity.Warning);

    public FlowValidationResult(IEnumerable<FlowValidationIssue> issues)
    {
        Issues = issues.ToList().AsReadOnly();
    }
}

