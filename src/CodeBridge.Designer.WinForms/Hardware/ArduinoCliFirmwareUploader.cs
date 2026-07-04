using System.Diagnostics;

namespace CodeBridge.Designer.WinForms.Hardware;

internal sealed class ArduinoCliFirmwareUploader : ProcessFirmwareUploader
{
    private readonly string _arduinoCliPath;
    private readonly string _fqbn;
    private readonly string _portName;
    private readonly string _sketchDirectory;

    public ArduinoCliFirmwareUploader(
        string arduinoCliPath,
        string fqbn,
        string portName,
        string sketchDirectory)
        : base(
            "Arduino CLI",
            "Install Arduino IDE 2.x or set CODEBRIDGE_ARDUINO_CLI to arduino-cli.exe.",
            sketchDirectory,
            $"\"{arduinoCliPath}\" compile --fqbn {fqbn} --upload -p {portName} \"{sketchDirectory}\"")
    {
        _arduinoCliPath = arduinoCliPath;
        _fqbn = fqbn;
        _portName = portName;
        _sketchDirectory = sketchDirectory;
    }

    protected override ProcessStartInfo CreateStartInfo()
    {
        var startInfo = HardwareProcessStartInfo.Create(_arduinoCliPath, WorkingDirectory);
        startInfo.ArgumentList.Add("compile");
        startInfo.ArgumentList.Add("--fqbn");
        startInfo.ArgumentList.Add(_fqbn);
        startInfo.ArgumentList.Add("--upload");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(_portName);
        startInfo.ArgumentList.Add(_sketchDirectory);
        return startInfo;
    }
}
