using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using CodeBridge.Transport.Serial;

namespace CodeBridge.Transport;

public static class BoardDiscovery
{
    public static PortInfo[] DiscoverPorts()
    {
        var ports = new Dictionary<string, PortInfo>(StringComparer.OrdinalIgnoreCase);

        // Get basic list from SerialPort
        foreach (var portName in SerialTransport.GetAvailablePorts())
        {
            ports[portName] = new PortInfo(portName);
        }

        if (OperatingSystem.IsWindows())
        {
            try
            {
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
                foreach (ManagementBaseObject queryObj in searcher.Get())
                {
                    var nameObj = queryObj["Name"];
                    if (nameObj == null) continue;

                    string name = nameObj.ToString()!;
                    var match = Regex.Match(name, @"\((COM\d+)\)");
                    if (match.Success)
                    {
                        string portName = match.Groups[1].Value;
                        string? deviceId = queryObj["PNPDeviceID"]?.ToString();
                        string? description = queryObj["Description"]?.ToString();
                        string? manufacturer = queryObj["Manufacturer"]?.ToString();

                        string? vid = null;
                        string? pid = null;

                        if (deviceId != null)
                        {
                            var vidMatch = Regex.Match(deviceId, @"VID_([0-9A-Fa-f]{4})");
                            var pidMatch = Regex.Match(deviceId, @"PID_([0-9A-Fa-f]{4})");
                            
                            if (vidMatch.Success) vid = vidMatch.Groups[1].Value;
                            if (pidMatch.Success) pid = pidMatch.Groups[1].Value;
                        }

                        var (boardHint, _) = UsbDeviceIdentifier.Identify(vid, pid);

                        ports[portName] = new PortInfo(portName)
                        {
                            Description = description,
                            VendorId = vid,
                            ProductId = pid,
                            Manufacturer = manufacturer,
                            BoardHint = boardHint
                        };
                    }
                }
            }
            catch
            {
                // Ignore WMI errors
            }
        }

        return ports.Values.OrderBy(p => p.Name).ToArray();
    }
}

public record PortInfo(string Name)
{
    public string? Description { get; init; }
    public string? VendorId { get; init; }
    public string? ProductId { get; init; }
    public string? Manufacturer { get; init; }
    public string? BoardHint { get; init; }

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(BoardHint))
        {
            return $"{Name} - {BoardHint}";
        }
        else if (!string.IsNullOrWhiteSpace(Description))
        {
            return $"{Name} - {Description}";
        }
        
        return Name;
    }
}
