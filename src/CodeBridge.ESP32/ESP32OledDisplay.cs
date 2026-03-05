using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Displays;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 OLED display driver for SSD1306 via I2C.
/// Supports 128x64 and 128x32 monochrome OLED displays.
/// Uses a framebuffer pattern: draw operations write to buffer, FlushAsync pushes to display.
/// </summary>
public class ESP32OledDisplay : IPixelDisplay
{
    private readonly ITransport _transport;
    private bool _disposed;

    /// <summary>I2C address of the OLED (default: 0x3C).</summary>
    public int Address { get; }

    public int Width { get; }
    public int Height { get; }
    public int ColorDepth => 1; // Monochrome

    public ESP32OledDisplay(ITransport transport, int width = 128, int height = 64, int address = 0x3C)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Width = width;
        Height = height;
        Address = address;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_INIT, Width, Height, Address), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED init failed: {error}");

    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_CLEAR), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED clear failed: {error}");
    }

    public async Task FlushAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_FLUSH), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED flush failed: {error}");
    }

    public async Task DrawPixelAsync(int x, int y, uint color, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_PIXEL, x, y, color > 0 ? 1 : 0), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED pixel failed: {error}");
    }

    public async Task DrawLineAsync(int x1, int y1, int x2, int y2, uint color, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_LINE, x1, y1, x2, y2, color > 0 ? 1 : 0), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED line failed: {error}");
    }

    public async Task DrawRectAsync(int x, int y, int width, int height, uint color, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_RECT, x, y, width, height, color > 0 ? 1 : 0, 0), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED rect failed: {error}");
    }

    public async Task FillRectAsync(int x, int y, int width, int height, uint color, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_RECT, x, y, width, height, color > 0 ? 1 : 0, 1), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED fill rect failed: {error}");
    }

    public async Task DrawCircleAsync(int cx, int cy, int radius, uint color, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_CIRCLE, cx, cy, radius, color > 0 ? 1 : 0), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED circle failed: {error}");
    }

    /// <summary>
    /// Draws text at (x,y) with the given size. Color is ignored (always white on monochrome).
    /// </summary>
    public async Task DrawTextAsync(int x, int y, string text, uint color, int size = 1, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_TEXT, x, y, size, text), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED text failed: {error}");
    }

    public Task InvertAsync(bool invert, CancellationToken ct = default)
    {
        // Not implemented in firmware yet; could use SSD1306 invert command
        return Task.CompletedTask;
    }

    public async Task SetBrightnessAsync(byte brightness, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_BRIGHT, brightness), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"OLED brightness failed: {error}");
    }

    public Task DrawBitmapAsync(int x, int y, int width, int height, byte[] data, CancellationToken ct = default)
    {
        // Bitmap drawing not yet implemented in firmware
        throw new NotSupportedException("Bitmap drawing not yet supported over protocol.");
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
