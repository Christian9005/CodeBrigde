using System.Diagnostics;

namespace CodeBridge.Designer.WinForms.Hardware;

internal sealed class PlatformIoFirmwareUploader : ProcessFirmwareUploader
{
    private readonly string _portName;

    public PlatformIoFirmwareUploader(string firmwareDirectory, string portName)
        : base(
            "PlatformIO CLI",
            "Install PlatformIO Core or flash this board with the native vendor tool.",
            firmwareDirectory,
            $"pio run -t upload --upload-port {portName}")
    {
        _portName = portName;
    }

    protected override ProcessStartInfo CreateStartInfo()
    {
        var startInfo = HardwareProcessStartInfo.Create("pio", WorkingDirectory);
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("-t");
        startInfo.ArgumentList.Add("upload");
        startInfo.ArgumentList.Add("--upload-port");
        startInfo.ArgumentList.Add(_portName);
        return startInfo;
    }
}
