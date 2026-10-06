using CodeBridge.Flow;

namespace CodeBridge.Designer.WinForms.Hardware;

internal sealed record BoardConnectionRequest(
    BoardProfile BoardProfile,
    CodeBridgeTransportMode TransportMode,
    string PortName,
    int BaudRate,
    string Host,
    int TcpPort,
    string? AccessToken = null,
    Action<string, string>? CommandObserver = null);
