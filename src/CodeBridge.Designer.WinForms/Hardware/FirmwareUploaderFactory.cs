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
                $"No firmware project was found for {request.BoardProfile.DisplayName}. Reinstall the CodeBridge.Designer.WinForms package (it ships the firmware) or set CODEBRIDGE_FIRMWARE_ROOT to a folder containing esp32-bridge and arduino-uno-bridge.");
        }

        if (string.Equals(request.BoardProfile.Id, BuiltInBoardProfiles.ArduinoUno.Id, StringComparison.OrdinalIgnoreCase))
            return CreateArduinoUnoUploader(firmwareDirectory, portName);

        var firmwareBin = HardwareToolLocator.ResolvePrebuiltFirmwarePath(firmwareDirectory, request.BoardProfile.Id);
        if (firmwareBin == null)
        {
            throw new FileNotFoundException($"Could not find prebuilt firmware.bin for {request.BoardProfile.DisplayName}. Please build the firmware first.");
        }

        var bootloaderPath = Path.Combine(Path.GetDirectoryName(firmwareBin) ?? string.Empty, "bootloader.bin");
        var partitionsPath = Path.Combine(Path.GetDirectoryName(firmwareBin) ?? string.Empty, "partitions.bin");

        if (!File.Exists(bootloaderPath)) bootloaderPath = null;
        if (!File.Exists(partitionsPath)) partitionsPath = null;
        var bootApp0Path = Path.Combine(Path.GetDirectoryName(firmwareBin) ?? string.Empty, "boot_app0.bin");
        if (!File.Exists(bootApp0Path)) bootApp0Path = null;

        return new EsptoolFirmwareUploader(
            HardwareToolLocator.ResolveEsptoolPath(),
            portName,
            firmwareBin,
            bootloaderPath,
            partitionsPath,
            bootApp0Path);
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
