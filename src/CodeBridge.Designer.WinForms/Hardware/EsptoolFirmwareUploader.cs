using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBridge.Designer.WinForms.Hardware;

internal class EsptoolFirmwareUploader : ProcessFirmwareUploader
{
    private string _esptoolPath;
    private readonly string _portName;
    private readonly string _firmwareBinPath;
    private readonly string? _bootloaderPath;
    private readonly string? _partitionsPath;
    private readonly string? _bootApp0Path;

    public EsptoolFirmwareUploader(
        string esptoolPath, 
        string portName, 
        string firmwareBinPath,
        string? bootloaderPath = null,
        string? partitionsPath = null,
        string? bootApp0Path = null)
        : base(
            "esptool", 
            "esptool was not found and could not be downloaded automatically. Check your internet connection or install esptool and add it to PATH.", 
            Path.GetDirectoryName(firmwareBinPath) ?? string.Empty, 
            "esptool.exe write_flash")
    {
        _esptoolPath = esptoolPath;
        _portName = portName;
        _firmwareBinPath = firmwareBinPath;
        _bootloaderPath = bootloaderPath;
        _partitionsPath = partitionsPath;
        _bootApp0Path = bootApp0Path;
    }

    protected override async Task PrepareAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_esptoolPath))
            return;

        // A bare command name may still resolve through PATH; otherwise download the pinned release.
        if (!Path.IsPathRooted(_esptoolPath) && HardwareToolLocator.IsOnPath(_esptoolPath))
            return;

        _esptoolPath = await HardwareToolLocator.EnsureEsptoolAsync(new Progress<string>(ReportOutput));
    }

    protected override ProcessStartInfo CreateStartInfo()
    {
        var arguments = $"--chip esp32 --port \"{_portName}\" --baud 460800 write_flash -z";

        if (!string.IsNullOrEmpty(_bootloaderPath) && !string.IsNullOrEmpty(_partitionsPath))
        {
            arguments += $" 0x1000 \"{_bootloaderPath}\" 0x8000 \"{_partitionsPath}\"";
            if (!string.IsNullOrEmpty(_bootApp0Path))
                arguments += $" 0xe000 \"{_bootApp0Path}\"";
        }

        arguments += $" 0x10000 \"{_firmwareBinPath}\"";

        return new ProcessStartInfo
        {
            FileName = _esptoolPath,
            Arguments = arguments,
            WorkingDirectory = WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }
}
