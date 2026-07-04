using CodeBridge.Flow;

namespace CodeBridge.Designer.WinForms.Hardware;

internal static class FirmwareUploaderFactory
{
    public static IBoardFirmwareUploader Create(FirmwareUploadRequest request)
    {
        var portName = request.PortName.Trim();
        if (string.IsNullOrWhiteSpace(portName))
            throw new InvalidOperationException("Select a serial port before uploading firmware.");

        var firmwareDirectory = HardwareToolLocator.ResolveFirmwareDirectory(request.BoardProfile.Id);
        if (firmwareDirectory is null)
        {
            throw new DirectoryNotFoundException(
                $"No firmware project was found for {request.BoardProfile.DisplayName}. Expected a firmware project under C:\\Projects\\CodeBridge\\firmware or CODEBRIDGE_FIRMWARE_ROOT.");
        }

        if (string.Equals(request.BoardProfile.Id, BuiltInBoardProfiles.ArduinoUno.Id, StringComparison.OrdinalIgnoreCase))
            return CreateArduinoUnoUploader(firmwareDirectory, portName);

        return new PlatformIoFirmwareUploader(firmwareDirectory, portName);
    }

    private static IBoardFirmwareUploader CreateArduinoUnoUploader(string firmwareDirectory, string portName)
    {
        var sketchDirectory = Path.Combine(firmwareDirectory, "CodeBridgeArduinoUno");
        if (!Directory.Exists(sketchDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Arduino Uno firmware sketch was not found. Expected: {sketchDirectory}");
        }

        return new ArduinoCliFirmwareUploader(
            HardwareToolLocator.ResolveArduinoCliPath(),
            "arduino:avr:uno",
            portName,
            sketchDirectory);
    }
}
