using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 OTA (Over-The-Air) firmware update controller.
/// Downloads and flashes firmware from an HTTP URL.
/// Requires WiFi connectivity.
/// </summary>
public class ESP32OtaController : IOtaController
{
    private readonly ITransport _transport;

    public ESP32OtaController(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task UpdateFromUrlAsync(string firmwareUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(firmwareUrl))
            throw new ArgumentException("Firmware URL is required.", nameof(firmwareUrl));

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OTA_BEGIN, firmwareUrl), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"OTA update failed: {data}");
        // Board will restart after successful OTA
    }

    public async Task<(string Status, int Progress)> GetStatusAsync(CancellationToken ct = default)
    {
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OTA_STATUS), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"OTA status failed: {data}");

        // Format: "status,progress"
        var parts = data.Split(',');
        var status = parts[0];
        var progress = parts.Length > 1 ? int.Parse(parts[1]) : 0;
        return (status, progress);
    }
}
