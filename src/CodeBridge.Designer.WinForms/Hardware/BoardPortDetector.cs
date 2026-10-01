using Microsoft.Win32;
using CodeBridge.Flow;

namespace CodeBridge.Designer.WinForms.Hardware;

internal static class BoardPortDetector
{
    public static BoardProfile? SuggestBoardForPort(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
            return null;

        var description = TryGetUsbPortDescription(portName);
        if (string.IsNullOrWhiteSpace(description))
            return null;

        if (Contains(description, "arduino") ||
            Contains(description, "vid_2341") ||
            Contains(description, "vid_2a03"))
        {
            return BuiltInBoardProfiles.ArduinoUno;
        }

        if (Contains(description, "esp32") ||
            Contains(description, "usb jtag") ||
            Contains(description, "silicon labs") ||
            Contains(description, "cp210") ||
            Contains(description, "vid_10c4") ||
            Contains(description, "vid_303a"))
        {
            return BuiltInBoardProfiles.Esp32DevKit;
        }

        return null;
    }

    /// <summary>Human readable USB description of a COM port (for example "Silicon Labs CP210x"), or null.</summary>
    public static string? DescribePort(string portName)
    {
        var description = TryGetUsbPortDescription(portName);
        if (string.IsNullOrWhiteSpace(description))
            return null;

        // "<friendly name> <VID_xxxx&PID_xxxx> <serial>": keep the friendly part.
        var vid = description.IndexOf("VID_", StringComparison.OrdinalIgnoreCase);
        return (vid > 0 ? description[..vid] : description).Trim();
    }

    private static bool Contains(string? value, string filter) =>
        value?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

    private static string? TryGetUsbPortDescription(string portName)
    {
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            foreach (var rootPath in new[] { @"SYSTEM\CurrentControlSet\Enum\USB", @"SYSTEM\CurrentControlSet\Enum\FTDIBUS" })
            {
                var description = TryGetUsbPortDescription(rootPath, portName);
                if (!string.IsNullOrWhiteSpace(description))
                    return description;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? TryGetUsbPortDescription(string rootPath, string portName)
    {
        using var root = Registry.LocalMachine.OpenSubKey(rootPath);
        if (root is null)
            return null;

        foreach (var deviceName in root.GetSubKeyNames())
        {
            using var device = root.OpenSubKey(deviceName);
            if (device is null)
                continue;

            foreach (var instanceName in device.GetSubKeyNames())
            {
                using var instance = device.OpenSubKey(instanceName);
                using var parameters = instance?.OpenSubKey("Device Parameters");
                var registeredPort = parameters?.GetValue("PortName")?.ToString();
                if (!string.Equals(registeredPort, portName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var friendlyName = instance?.GetValue("FriendlyName")?.ToString();
                return $"{friendlyName} {deviceName} {instanceName}";
            }
        }

        return null;
    }
}
