using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 power management — deep sleep with timer or GPIO wake-up.
/// After deep sleep, the board resets and runs setup() again.
/// All RAM state is lost; use Preferences/NVS for persistent data.
/// </summary>
public class ESP32PowerController : IPowerController
{
    private readonly ITransport _transport;

    public ESP32PowerController(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task DeepSleepAsync(TimeSpan duration, CancellationToken ct = default)
    {
        int seconds = (int)duration.TotalSeconds;
        if (seconds < 1 || seconds > 86400)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be 1 second to 24 hours.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DEEP_SLEEP, seconds), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Deep sleep failed: {data}");
        // Board is now sleeping — connection will be lost
    }

    public async Task DeepSleepUntilPinAsync(int wakePin, bool wakeOnHigh = true, CancellationToken ct = default)
    {
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DEEP_SLEEP_PIN, wakePin, wakeOnHigh ? 1 : 0), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Deep sleep pin failed: {data}");
        // Board is now sleeping — connection will be lost
    }
}
