namespace CodeBridge.Designer.WinForms.Hardware;

internal interface IBoardFirmwareUploader
{
    event Action<string>? OutputReceived;
    string ToolName { get; }
    string MissingToolMessage { get; }
    string WorkingDirectory { get; }
    string CommandText { get; }
    Task<FirmwareUploadResult> UploadAsync(CancellationToken cancellationToken = default);
}
