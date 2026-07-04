namespace CodeBridge.Designer.WinForms.Hardware;

public sealed record FirmwareUploadResult(
    bool Success,
    string ToolName,
    string CommandText,
    string WorkingDirectory,
    string Log,
    int? ExitCode = null)
{
    public static FirmwareUploadResult Failed(
        string toolName,
        string commandText,
        string workingDirectory,
        string log,
        int? exitCode = null) =>
        new(false, toolName, commandText, workingDirectory, log, exitCode);
}
