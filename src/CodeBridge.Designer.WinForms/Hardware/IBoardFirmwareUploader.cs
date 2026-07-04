namespace CodeBridge.Designer.WinForms.Hardware;

internal interface IBoardFirmwareUploader
{
    string ToolName { get; }
    string MissingToolMessage { get; }
    string WorkingDirectory { get; }
    string CommandText { get; }
    Task<FirmwareUploadResult> UploadAsync(CancellationToken cancellationToken = default);
}
