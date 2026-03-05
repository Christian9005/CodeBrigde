using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Displays;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 LCD display driver for I2C HD44780 character displays (16x2, 20x4).
/// Uses PCF8574 I2C backpack (default address: 0x27).
/// </summary>
public class ESP32LcdDisplay : ICharacterDisplay
{
    private readonly ITransport _transport;
    private bool _disposed;

    /// <summary>I2C address of the LCD backpack (default: 0x27).</summary>
    public int Address { get; }

    public int Width => Columns;
    public int Height => Rows;
    public int Columns { get; }
    public int Rows { get; }

    public ESP32LcdDisplay(ITransport transport, int columns = 16, int rows = 2, int address = 0x27)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Columns = columns;
        Rows = rows;
        Address = address;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_LCD_INIT, Address, Columns, Rows), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"LCD init failed: {error}");

    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_LCD_CLEAR), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"LCD clear failed: {error}");
    }

    public Task FlushAsync(CancellationToken ct = default)
    {
        // LCD writes immediately — no buffer to flush
        return Task.CompletedTask;
    }

    public async Task WriteTextAsync(int row, int column, string text, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_LCD_TEXT, row, column, text), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"LCD text failed: {error}");
    }

    public async Task SetCursorAsync(int row, int column, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_LCD_CURSOR, row, column), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"LCD cursor failed: {error}");
    }

    public async Task SetBacklightAsync(bool on, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_LCD_BACKLIGHT, on ? 1 : 0), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"LCD backlight failed: {error}");
    }

    public Task ScrollAsync(ScrollDirection direction, CancellationToken ct = default)
    {
        // Scroll not yet implemented in firmware
        throw new NotSupportedException("LCD scroll not yet supported over protocol.");
    }

    public Task CreateCustomCharAsync(int location, byte[] pattern, CancellationToken ct = default)
    {
        // Custom characters not yet implemented in firmware
        throw new NotSupportedException("Custom characters not yet supported over protocol.");
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
