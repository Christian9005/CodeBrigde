using CodeBridge.Flow;

namespace CodeBridge.Designer.WinForms.Hardware;

internal static class HardwareToolLocator
{
    public static string ResolveEsptoolPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsDir = Path.Combine(localAppData, "CodeBridge", "tools", "esptool");
        
        if (Directory.Exists(toolsDir))
        {
            var exes = Directory.GetFiles(toolsDir, "esptool.exe", SearchOption.AllDirectories);
            if (exes.Length > 0)
                return exes[0];
        }

        var idfPath = Environment.GetEnvironmentVariable("IDF_PATH");
        if (!string.IsNullOrWhiteSpace(idfPath))
        {
            var idfEsptool = Path.Combine(idfPath, "components", "esptool_py", "esptool", "esptool.exe");
            if (File.Exists(idfEsptool))
                return idfEsptool;
                
            var idfEsptoolPy = Path.Combine(idfPath, "components", "esptool_py", "esptool", "esptool.py");
            if (File.Exists(idfEsptoolPy))
                return idfEsptoolPy;
        }

        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach (var path in paths)
        {
            var pythonEsptool = Path.Combine(path, "esptool.exe");
            if (File.Exists(pythonEsptool))
                return pythonEsptool;
                
            var pythonEsptoolPy = Path.Combine(path, "esptool.py");
            if (File.Exists(pythonEsptoolPy))
                return pythonEsptoolPy;
        }

        return "esptool";
    }

    // Pinned esptool release, downloaded on demand over HTTPS and verified by SHA-256.
    private const string EsptoolVersion = "v5.4.0";
    private const string EsptoolUrl = "https://github.com/espressif/esptool/releases/download/v5.4.0/esptool-v5.4.0-windows-amd64.zip";
    private const string EsptoolSha256 = "b7f6b9dd301a210b31f4829118c909c84aae23107f9ca1fdc14ccf4d7384be2e";

    public static Task<string> EnsureEsptoolAsync(IProgress<string>? progress = null)
    {
        return NativeToolManager.DownloadAndInstallToolAsync(
            "esptool",
            EsptoolVersion,
            EsptoolUrl,
            EsptoolSha256,
            "esptool.exe",
            progress);
    }

    public static bool IsOnPath(string commandName)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            try
            {
                if (File.Exists(Path.Combine(path, commandName + ".exe")) || File.Exists(Path.Combine(path, commandName)))
                    return true;
            }
            catch (ArgumentException)
            {
            }
        }

        return false;
    }

    public static string? ResolvePrebuiltFirmwarePath(string firmwareDirectory, string boardProfileId)
    {
        // Shipped binaries: <root>\prebuilt\<chip>\firmware.bin (root = parent of the firmware project folder).
        var chip = BuiltInBoardProfiles.FindById(boardProfileId)?.PrebuiltFolder ?? "esp32";
        var root = Path.GetDirectoryName(firmwareDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (root is not null)
        {
            var shipped = Path.Combine(root, "prebuilt", chip, "firmware.bin");
            if (File.Exists(shipped))
                return shipped;
        }

        var prebuilt = Path.Combine(firmwareDirectory, "prebuilt", "firmware.bin");
        if (File.Exists(prebuilt))
            return prebuilt;

        var pioBin = Path.Combine(firmwareDirectory, ".pio", "build", chip, "firmware.bin");
        if (File.Exists(pioBin))
            return pioBin;

        return null;
    }

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
        var projectName = BuiltInBoardProfiles.FindById(boardProfileId)?.FirmwareProject;

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

        var assemblyDirectory = Path.GetDirectoryName(typeof(HardwareToolLocator).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            // Next to the assembly (copied by the NuGet build targets) and in the NuGet package root (lib\<tfm>\ -> ..\..\firmware).
            yield return Path.Combine(assemblyDirectory, "firmware");
            yield return Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "firmware"));
        }

        yield return Path.Combine(AppContext.BaseDirectory, "firmware");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodeBridge",
            "firmware");
    }
}
