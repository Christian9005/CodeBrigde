using CodeBridge.Core.Enums;

namespace CodeBridge.Core.Abstractions.Displays;

/// <summary>
/// Base interface for all display devices.
/// </summary>
public interface IDisplay : IDisposable
{
    /// <summary>
    /// Display width in pixels (or columns for character displays).
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Display height in pixels (or rows for character displays).
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Initializes the display hardware.
    /// </summary>
    Task InitAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears the entire display.
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Flushes the internal buffer to the physical display.
    /// </summary>
    Task FlushAsync(CancellationToken ct = default);
}

/// <summary>
/// Character-based display (LCD 16x2, 20x4, etc.) typically via I2C (PCF8574).
/// </summary>
public interface ICharacterDisplay : IDisplay
{
    /// <summary>
    /// Number of character columns (typically 16 or 20).
    /// </summary>
    int Columns { get; }

    /// <summary>
    /// Number of rows (typically 2 or 4).
    /// </summary>
    int Rows { get; }

    /// <summary>
    /// Writes text at a specific row and column.
    /// </summary>
    Task WriteTextAsync(int row, int column, string text, CancellationToken ct = default);

    /// <summary>
    /// Sets the cursor position.
    /// </summary>
    Task SetCursorAsync(int row, int column, CancellationToken ct = default);

    /// <summary>
    /// Turns the backlight on or off.
    /// </summary>
    Task SetBacklightAsync(bool on, CancellationToken ct = default);

    /// <summary>
    /// Scrolls the display left or right.
    /// </summary>
    Task ScrollAsync(ScrollDirection direction, CancellationToken ct = default);

    /// <summary>
    /// Defines a custom character at one of 8 CGRAM slots (0-7).
    /// </summary>
    Task CreateCustomCharAsync(int location, byte[] pattern, CancellationToken ct = default);
}

/// <summary>
/// Pixel-based display (OLED SSD1306, TFT ILI9341, E-Paper).
/// </summary>
public interface IPixelDisplay : IDisplay
{
    /// <summary>
    /// Color depth in bits per pixel.
    /// </summary>
    int ColorDepth { get; }

    /// <summary>
    /// Draws a single pixel.
    /// </summary>
    Task DrawPixelAsync(int x, int y, uint color, CancellationToken ct = default);

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    Task DrawLineAsync(int x1, int y1, int x2, int y2, uint color, CancellationToken ct = default);

    /// <summary>
    /// Draws a rectangle outline.
    /// </summary>
    Task DrawRectAsync(int x, int y, int width, int height, uint color, CancellationToken ct = default);

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    Task FillRectAsync(int x, int y, int width, int height, uint color, CancellationToken ct = default);

    /// <summary>
    /// Draws a circle outline.
    /// </summary>
    Task DrawCircleAsync(int cx, int cy, int radius, uint color, CancellationToken ct = default);

    /// <summary>
    /// Draws text at a position with the specified size (1-4).
    /// </summary>
    Task DrawTextAsync(int x, int y, string text, uint color, int size = 1, CancellationToken ct = default);

    /// <summary>
    /// Inverts the display colors.
    /// </summary>
    Task InvertAsync(bool invert, CancellationToken ct = default);

    /// <summary>
    /// Sets the display brightness/contrast (0-255).
    /// </summary>
    Task SetBrightnessAsync(byte brightness, CancellationToken ct = default);

    /// <summary>
    /// Draws a bitmap image from raw pixel data.
    /// </summary>
    Task DrawBitmapAsync(int x, int y, int width, int height, byte[] data, CancellationToken ct = default);
}
