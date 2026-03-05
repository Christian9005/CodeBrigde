// ═══════════════════════════════════════════════════════════════
//  CodeBridge Sample: Servo Sweep
//  ──────────────────────────────
//  Smoothly sweeps a servo motor from 0° to 180° and back.
//  Demonstrates the new Phase 3 actuator drivers.
//
//  Hardware: Connect a servo signal wire to GPIO 13
//            (VCC to 5V, GND to GND)
//  Firmware: CodeBridge firmware v0.3.0+
// ═══════════════════════════════════════════════════════════════

using CodeBridge.Core;
using CodeBridge.ESP32;
using CodeBridge.Transport;

Console.WriteLine("CodeBridge - Servo Sweep Sample");
Console.WriteLine("──────────────────────────────────");

var ports = BoardDiscovery.DiscoverPorts();
if (ports.Length == 0)
{
    Console.WriteLine("No serial ports found. Connect your ESP32 and try again.");
    return;
}

Console.WriteLine("Available ports:");
for (int i = 0; i < ports.Length; i++)
    Console.WriteLine($"  [{i}] {ports[i].Name}");

Console.Write("\nSelect port number (0): ");
var input = Console.ReadLine();
var portIndex = string.IsNullOrEmpty(input) ? 0 : int.Parse(input);
var selectedPort = ports[portIndex].Name;

// ── Connect ──────────────────────────────────────────────────
Console.WriteLine($"\nConnecting to ESP32 on {selectedPort}...");

await using var board = await CodeBridgeBuilder
    .Connect()
    .Serial(selectedPort)
    .ToESP32()
    .BuildAsync();

Console.WriteLine($"Connected! Firmware: {board.FirmwareVersion}");

// ── Create servo on GPIO 13 ─────────────────────────────────
const int servoPin = 13;
using var servo = board.CreateServo(servoPin);

Console.WriteLine($"\nAttaching servo on GPIO {servoPin}...");
await servo.InitAsync();
Console.WriteLine("Servo ready!");

// ── Sweep loop ───────────────────────────────────────────────
Console.WriteLine("\nSweeping servo 0° → 180° → 0°  (Press Ctrl+C to stop)\n");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

int sweepCount = 0;
try
{
    while (!cts.Token.IsCancellationRequested)
    {
        sweepCount++;

        // Sweep 0° → 180°
        for (int angle = 0; angle <= 180; angle += 5)
        {
            await servo.SetAngleAsync(angle);
            Console.Write($"\r  Sweep #{sweepCount}  Angle: {angle,3}°  ");
            await Task.Delay(30, cts.Token);
        }

        // Sweep 180° → 0°
        for (int angle = 180; angle >= 0; angle -= 5)
        {
            await servo.SetAngleAsync(angle);
            Console.Write($"\r  Sweep #{sweepCount}  Angle: {angle,3}°  ");
            await Task.Delay(30, cts.Token);
        }
    }
}
catch (OperationCanceledException) { }

// ── Cleanup ──────────────────────────────────────────────────
Console.WriteLine($"\n\nDetaching servo...");
await servo.DetachAsync();
Console.WriteLine($"Done! Completed {sweepCount} sweep(s).");
