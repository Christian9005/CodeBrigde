using CodeBridge.Transport.Serial;

namespace CodeBridge.Transport;

/// <summary>
/// Helper for discovering connected boards.
/// </summary>
public static class BoardDiscovery
{
    /// <summary>
    /// Lists all available serial ports with descriptive info.
    /// </summary>
    public static PortInfo[] DiscoverPorts()
    {
        return SerialTransport.GetAvailablePorts()
            .Select(p => new PortInfo(p))
            .ToArray();
    }
}

/// <summary>
/// Information about a serial port.
/// </summary>
public record PortInfo(string Name)
{
    public override string ToString() => Name;
}
