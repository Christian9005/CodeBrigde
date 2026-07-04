using System.ComponentModel;
using System.Diagnostics;

namespace CodeBridge.Designer.WinForms.Hardware;

internal abstract class ProcessFirmwareUploader : IBoardFirmwareUploader
{
    protected ProcessFirmwareUploader(
        string toolName,
        string missingToolMessage,
        string workingDirectory,
        string commandText)
    {
        ToolName = toolName;
        MissingToolMessage = missingToolMessage;
        WorkingDirectory = workingDirectory;
        CommandText = commandText;
    }

    public string ToolName { get; }
    public string MissingToolMessage { get; }
    public string WorkingDirectory { get; }
    public string CommandText { get; }

    public async Task<FirmwareUploadResult> UploadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = CreateStartInfo(),
                EnableRaisingEvents = false
            };

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;
            var log = string.Join(Environment.NewLine, output, error).Trim();

            return new FirmwareUploadResult(
                process.ExitCode == 0,
                ToolName,
                CommandText,
                WorkingDirectory,
                log,
                process.ExitCode);
        }
        catch (Win32Exception)
        {
            return FirmwareUploadResult.Failed(
                ToolName,
                CommandText,
                WorkingDirectory,
                $"{ToolName} was not found.{Environment.NewLine}{MissingToolMessage}");
        }
    }

    protected abstract ProcessStartInfo CreateStartInfo();
}
