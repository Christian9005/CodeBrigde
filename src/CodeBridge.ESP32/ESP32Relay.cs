using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Actuators;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 relay driver. Uses GPIO digital write to control single or multi-channel relay modules.
/// Active-LOW relays (most common): ON = LOW, OFF = HIGH.
/// Active-HIGH relays: ON = HIGH, OFF = LOW.
/// </summary>
public class ESP32Relay : IRelay
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    /// <summary>Whether the relay is active-LOW (most relay modules are).</summary>
    public bool ActiveLow { get; }

    public string Name => $"Relay (pin {Pin})";
    public bool IsReady => _initialized;
    public bool IsOn { get; private set; }
    public int Pin { get; }

    public ESP32Relay(ITransport transport, int pin, bool activeLow = true)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Pin = pin;
        ActiveLow = activeLow;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Set pin as OUTPUT
        await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PIN_MODE, Pin, 1), ct);

        // Start OFF
        int offValue = ActiveLow ? 1 : 0;
        await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DIGITAL_WRITE, Pin, offValue), ct);

        _initialized = true;
        IsOn = false;
    }

    public async Task OnAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int onValue = ActiveLow ? 0 : 1;
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DIGITAL_WRITE, Pin, onValue), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Relay ON failed: {error}");

        IsOn = true;
    }

    public async Task OffAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int offValue = ActiveLow ? 1 : 0;
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DIGITAL_WRITE, Pin, offValue), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Relay OFF failed: {error}");

        IsOn = false;
    }

    public async Task ToggleAsync(CancellationToken ct = default)
    {
        if (IsOn) await OffAsync(ct);
        else await OnAsync(ct);
    }

    public async Task PulseAsync(TimeSpan duration, CancellationToken ct = default)
    {
        await OnAsync(ct);
        await Task.Delay(duration, ct);
        await OffAsync(ct);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
