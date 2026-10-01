// Example 4 - Servo sweep
// Same as the flow:  Servo Write 0 -> Timer -> Servo Write 180 -> Timer -> Servo Write 90
using CodeBridge.ESP32;

namespace CodeBridge.Samples.TourSnippets;

public static class Example4_Servo
{
    public static async Task RunAsync(ESP32Board board, CancellationToken ct = default)
    {
        using var servo = board.CreateServo(pin: 13);   // signal on GPIO 13, power from an external 5 V supply
        await servo.InitAsync(ct);

        await servo.SetAngleAsync(0, ct);
        await Task.Delay(700, ct);
        await servo.SetAngleAsync(180, ct);
        await Task.Delay(700, ct);
        await servo.SetAngleAsync(90, ct);
    }
}
