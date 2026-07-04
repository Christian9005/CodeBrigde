using CodeBridge.Flow;

namespace CodeBridge.Designer.WinForms.Hardware;

internal static class HardwareToolLocator
{
    public static string ResolveArduinoCliPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("CODEBRIDGE_ARDUINO_CLI");
        if (File.Exists(configuredPath))
            return configuredPath!;

        foreach (var candidate in EnumerateArduinoCliCandidates())
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return "arduino-cli";
    }

    public static string? ResolveFirmwareDirectory(string boardProfileId)
    {
        var projectName = boardProfileId switch
        {
            var id when string.Equals(id, BuiltInBoardProfiles.Esp32DevKit.Id, StringComparison.OrdinalIgnoreCase) => "esp32-bridge",
            var id when string.Equals(id, BuiltInBoardProfiles.ArduinoUno.Id, StringComparison.OrdinalIgnoreCase) => "arduino-uno-bridge",
            _ => null
        };

        if (projectName is null)
            return null;

        foreach (var root in EnumerateFirmwareRoots())
        {
            var candidate = Path.Combine(root, projectName);
            if (Directory.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateArduinoCliCandidates()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        if (!string.IsNullOrWhiteSpace(localAppData))
            yield return Path.Combine(localAppData, "Programs", "Arduino IDE", "resources", "app", "lib", "backend", "resources", "arduino-cli.exe");

        if (!string.IsNullOrWhiteSpace(programFiles))
            yield return Path.Combine(programFiles, "Arduino IDE", "resources", "app", "lib", "backend", "resources", "arduino-cli.exe");

        if (!string.IsNullOrWhiteSpace(programFilesX86))
            yield return Path.Combine(programFilesX86, "Arduino IDE", "resources", "app", "lib", "backend", "resources", "arduino-cli.exe");
    }

    private static IEnumerable<string> EnumerateFirmwareRoots()
    {
        var environmentRoot = Environment.GetEnvironmentVariable("CODEBRIDGE_FIRMWARE_ROOT");
        if (!string.IsNullOrWhiteSpace(environmentRoot))
            yield return environmentRoot;

        yield return @"C:\Projects\CodeBridge\firmware";
        yield return Path.Combine(AppContext.BaseDirectory, "firmware");
    }
}
