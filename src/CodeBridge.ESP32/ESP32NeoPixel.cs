using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Actuators;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 NeoPixel (WS2812B) LED strip driver via Adafruit NeoPixel library.
/// Supports individually addressable RGB LEDs on any GPIO pin.
/// </summary>
public class ESP32NeoPixel : ILedStrip
{
    private readonly ITransport _transport;
    private bool _disposed;
    private bool _initialized;

    public string Name => $"NeoPixel ({LedCount} LEDs, pin {Pin})";
    public bool IsReady => _initialized;
    public int LedCount { get; }
    public int Pin { get; }

    public ESP32NeoPixel(ITransport transport, int pin, int ledCount)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));

        if (ledCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(ledCount), "LED count must be positive.");

        Pin = pin;
        LedCount = ledCount;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_INIT, LedCount, Pin), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"NeoPixel init failed: {error}");

        _initialized = true;
    }

    public async Task SetPixelAsync(int index, byte r, byte g, byte b, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (index < 0 || index >= LedCount)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-{LedCount - 1}.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_SET, index, r, g, b), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"NeoPixel set pixel failed: {error}");
    }

    public async Task SetAllAsync(byte r, byte g, byte b, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_ALL, r, g, b), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"NeoPixel set all failed: {error}");
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_CLEAR), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"NeoPixel clear failed: {error}");
    }

    public async Task ShowAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_SHOW), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"NeoPixel show failed: {error}");
    }

    public async Task SetBrightnessAsync(byte brightness, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_BRIGHT, brightness), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"NeoPixel brightness failed: {error}");
    }

    public async Task SetRangeAsync(int startIndex, (byte R, byte G, byte B)[] colors, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (startIndex < 0 || startIndex >= LedCount)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (startIndex + colors.Length > LedCount)
            throw new ArgumentException("Color array exceeds strip length.");

        // Build hex string: RRGGBBRRGGBB...
        var hexColors = string.Concat(colors.Select(c => $"{c.R:X2}{c.G:X2}{c.B:X2}"));

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_RANGE, startIndex, colors.Length, hexColors), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"NeoPixel set range failed: {error}");
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
