// Example 5 - Button controls LED
// Same as the flow:  Pin Mode (GPIO 4, input pull-down) -> Digital Read -> Digital Write. Enable "Loop" in the designer.
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Enums;

namespace CodeBridge.Samples.TourSnippets;

public static class Example5_Button
{
    public static async Task RunAsync(IBoard board, CancellationToken ct = default)
    {
        const int button = 4;                    // push button between 3V3 and GPIO 4
        const int led = 2;

        await board.Gpio.SetPinModeAsync(button, PinMode.InputPullDown, ct);
        await board.Gpio.SetPinModeAsync(led, PinMode.Output, ct);

        while (!ct.IsCancellationRequested)
        {
            PinValue pressed = await board.Gpio.DigitalReadAsync(button, ct);
            await board.Gpio.DigitalWriteAsync(led, pressed, ct);   // the LED follows the button
            await Task.Delay(20, ct);
        }
    }
}
