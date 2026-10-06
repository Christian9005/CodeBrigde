namespace CodeBridge.Core.Enums;

/// <summary>
/// Communication protocols supported by CodeBridge.
/// </summary>
public enum TransportProtocol
{
    Serial,
    WiFi,
    Bluetooth,
    MQTT,

    /// <summary>A virtual board that speaks the firmware protocol: no hardware needed.</summary>
    Simulator
}
