// Example 3 - Night light (sensor)
// Same as the flow:  Analog Read -> Compare (< 2000) -> Digital Write, with a Debug print. Enable "Loop" in the designer.
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Enums;

namespace CodeBridge.Samples.TourSnippets;

public static class Example3_NightLight
{
    public static async Task RunAsync(IBoard board, CancellationToken ct = default)
    {
        const int sensor = 34;                   // LDR or potentiometer (ADC1 pin)
        const int led = 2;
        const int threshold = 2000;              // 0 (0 V) ... 4095 (3.3 V)

        await board.Gpio.SetPinModeAsync(led, PinMode.Output, ct);

        while (!ct.IsCancellationRequested)      // this loop is what the "Loop" checkbox does for you
        {
            int light = await board.Gpio.AnalogReadAsync(sensor, ct);
            bool dark = light < threshold;

            await board.Gpio.DigitalWriteAsync(led, dark ? PinValue.High : PinValue.Low, ct);
            Console.WriteLine($"light = {light} -> LED {(dark ? "on" : "off")}");

            await Task.Delay(1000, ct);
        }
    }
}
