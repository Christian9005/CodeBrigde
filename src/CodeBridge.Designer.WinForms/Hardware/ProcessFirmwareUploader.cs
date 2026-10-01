using System.ComponentModel;
using System.Text;
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

    /// <summary>Raised for every line (or carriage-return progress update) the tool prints while it runs.</summary>
    public event Action<string>? OutputReceived;

    protected void ReportOutput(string text) => OutputReceived?.Invoke(text);

    public string ToolName { get; }
    public string MissingToolMessage { get; }
    public string WorkingDirectory { get; }
    public string CommandText { get; }

    public async Task<FirmwareUploadResult> UploadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await PrepareAsync(cancellationToken);

            using var process = new Process
            {
                StartInfo = CreateStartInfo(),
                EnableRaisingEvents = false
            };

            process.Start();
            try
            {
                var outputTask = PumpAsync(process.StandardOutput, cancellationToken);
                var errorTask = PumpAsync(process.StandardError, cancellationToken);
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
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore kill errors if already terminating
                }
                throw;
            }
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

    // Reads character by character so progress that ends in '\r' (esptool) is reported live, not only at exit.
    private async Task<string> PumpAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var all = new StringBuilder();
        var line = new StringBuilder();
        var buffer = new char[256];
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                var c = buffer[i];
                all.Append(c);
                if (c is '\n' or '\r')
                {
                    Flush(line);
                }
                else
                {
                    line.Append(c);
                }
            }
        }

        Flush(line);
        return all.ToString();
    }

    private void Flush(StringBuilder line)
    {
        if (line.Length == 0)
            return;

        var text = line.ToString().TrimEnd();
        line.Clear();
        if (text.Length > 0)
            OutputReceived?.Invoke(text);
    }

    /// <summary>Hook to acquire tools (for example download esptool) before the process starts.</summary>
    protected virtual Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected abstract ProcessStartInfo CreateStartInfo();
}
