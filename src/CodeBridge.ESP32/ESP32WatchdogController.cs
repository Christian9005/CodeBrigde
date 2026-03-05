using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 watchdog timer controller.
/// If the watchdog is not fed within the timeout, the MCU resets.
/// Useful for detecting firmware hangs in unattended deployments.
/// </summary>
public class ESP32WatchdogController : IWatchdogController
{
    private readonly ITransport _transport;

    public bool IsEnabled { get; private set; }

    public ESP32WatchdogController(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task EnableAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        int ms = (int)timeout.TotalMilliseconds;
        if (ms < 1000 || ms > 120000)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be 1-120 seconds.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_WDT_INIT, ms), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Watchdog init failed: {error}");
        IsEnabled = true;
    }

    public async Task FeedAsync(CancellationToken ct = default)
    {
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_WDT_FEED), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Watchdog feed failed: {error}");
    }

    public async Task DisableAsync(CancellationToken ct = default)
    {
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_WDT_DISABLE), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Watchdog disable failed: {error}");
        IsEnabled = false;
    }
}
