using System.Diagnostics;

namespace CodeBridge.Designer.WinForms.Hardware;

internal static class HardwareProcessStartInfo
{
    public static ProcessStartInfo Create(string fileName, string workingDirectory) =>
        new()
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
}
