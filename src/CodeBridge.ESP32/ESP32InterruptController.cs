using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 GPIO interrupt controller — edge detection via polling.
/// Supports up to 8 simultaneous interrupt pins.
/// </summary>
public class ESP32InterruptController : IGpioInterruptController
{
    private readonly ITransport _transport;
    private bool _disposed;
    private readonly HashSet<int> _attachedPins = new();

    public ESP32InterruptController(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task AttachInterruptAsync(int pin, InterruptEdge edge, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_INT_ATTACH, pin, (int)edge), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Interrupt attach failed: {error}");
        _attachedPins.Add(pin);
    }

    public async Task DetachInterruptAsync(int pin, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_INT_DETACH, pin), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Interrupt detach failed: {error}");
        _attachedPins.Remove(pin);
    }

    public async Task<IReadOnlyList<InterruptEvent>> PollAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_INT_POLL), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"Interrupt poll failed: {data}");

        if (data == "NONE") return Array.Empty<InterruptEvent>();

        var events = new List<InterruptEvent>();
        // Format: pin1:count1:edge1,pin2:count2:edge2,...
        var entries = data.Split(',');
        foreach (var entry in entries)
        {
            var parts = entry.Split(':');
            if (parts.Length >= 3)
            {
                var pin = int.Parse(parts[0]);
                var count = uint.Parse(parts[1]);
                var edge = (InterruptEdge)int.Parse(parts[2]);
                events.Add(new InterruptEvent(pin, count, edge));
            }
        }
        return events;
    }

    public async IAsyncEnumerable<InterruptEvent> WatchAsync(
        int pin, TimeSpan pollInterval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var events = await PollAsync(ct);
            foreach (var evt in events)
            {
                if (evt.Pin == pin)
                    yield return evt;
            }
            await Task.Delay(pollInterval, ct);
        }
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
