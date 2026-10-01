// Example 2 - Blink pattern
// Same as the flow:  Blink -> Timer -> Blink -> Timer -> Blink  (short, short, long)
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Enums;

namespace CodeBridge.Samples.TourSnippets;

public static class Example2_Pattern
{
    public static async Task RunAsync(IBoard board, CancellationToken ct = default)
    {
        const int led = 2;
        await board.Gpio.SetPinModeAsync(led, PinMode.Output, ct);

        await BlinkAsync(board, led, 200, ct);
        await Task.Delay(200, ct);
        await BlinkAsync(board, led, 200, ct);
        await Task.Delay(200, ct);
        await BlinkAsync(board, led, 700, ct);
    }

    private static async Task BlinkAsync(IBoard board, int pin, int milliseconds, CancellationToken ct)
    {
        await board.Gpio.DigitalWriteAsync(pin, PinValue.High, ct);
        await Task.Delay(milliseconds, ct);
        await board.Gpio.DigitalWriteAsync(pin, PinValue.Low, ct);
    }
}
