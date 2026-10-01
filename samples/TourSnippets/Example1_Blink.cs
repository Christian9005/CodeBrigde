// Example 1 - Blink an LED
// Same as the flow:  Manual Trigger -> Blink LED (GPIO 2, 500 ms)
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Enums;

namespace CodeBridge.Samples.TourSnippets;

public static class Example1_Blink
{
    public static async Task RunAsync(IBoard board, CancellationToken ct = default)
    {
        const int led = 2;                       // the on-board LED of most ESP32 DevKit boards

        await board.Gpio.SetPinModeAsync(led, PinMode.Output, ct);
        await board.Gpio.DigitalWriteAsync(led, PinValue.High, ct);   // on
        await Task.Delay(500, ct);
        await board.Gpio.DigitalWriteAsync(led, PinValue.Low, ct);    // off
    }
}
