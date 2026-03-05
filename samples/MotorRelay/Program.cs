// ═══════════════════════════════════════════════════════════════
//  CodeBridge Sample: Motor + Relay Control
//  ─────────────────────────────────────────
//  Demonstrates Phase 4 actuators: DC motor via H-Bridge
//  and relay switching.
//
//  Hardware:
//    DC Motor via L298N H-Bridge:
//      IN1 → GPIO 25, IN2 → GPIO 26, EN  → GPIO 27
//    Relay module (active-low):
//      Signal → GPIO 14
//
//  Firmware: CodeBridge firmware v0.4.0+
// ═══════════════════════════════════════════════════════════════

using CodeBridge.Core;
using CodeBridge.ESP32;
using CodeBridge.Transport;

Console.WriteLine("CodeBridge - Motor + Relay Sample");
Console.WriteLine("────────────────────────────────────");

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

Console.WriteLine($"Connected! Firmware: v{board.FirmwareVersion}");

// ── Create peripherals ──────────────────────────────────────
const int motorIn1 = 25, motorIn2 = 26, motorEnable = 27;
const int relayPin = 14;

using var motor = board.CreateMotor(motorIn1, motorIn2, motorEnable);
using var relay = board.CreateRelay(relayPin, activeLow: true);

Console.WriteLine("\nInitializing motor (L298N on GPIO 25/26/27)...");
await motor.InitAsync();

Console.WriteLine("Initializing relay (GPIO 14, active-low)...");
await relay.InitAsync();

Console.WriteLine("Ready!\n");

// ── Demo sequence ────────────────────────────────────────────

// 1) Relay toggle demo
Console.WriteLine("═══ Relay Demo ═══");
Console.WriteLine("  Turning relay ON...");
await relay.OnAsync();
await Task.Delay(1000);

Console.WriteLine("  Turning relay OFF...");
await relay.OffAsync();
await Task.Delay(500);

Console.WriteLine("  Pulsing relay (500ms)...");
await relay.PulseAsync(TimeSpan.FromMilliseconds(500));
await Task.Delay(500);

// 2) Motor speed ramp demo
Console.WriteLine("\n═══ Motor Demo ═══");
Console.WriteLine("  Ramping forward 0% → 100%...");
for (int speed = 0; speed <= 100; speed += 10)
{
    await motor.SetSpeedAsync(speed);
    Console.Write($"\r  Forward: {speed,3}%  ");
    await Task.Delay(300);
}
Console.WriteLine();

Console.WriteLine("  Braking...");
await motor.BrakeAsync();
await Task.Delay(500);

Console.WriteLine("  Ramping reverse 0% → 100%...");
for (int speed = 0; speed >= -100; speed -= 10)
{
    await motor.SetSpeedAsync(speed);
    Console.Write($"\r  Reverse: {Math.Abs(speed),3}%  ");
    await Task.Delay(300);
}
Console.WriteLine();

Console.WriteLine("  Braking...");
await motor.BrakeAsync();

// 3) Relay + Motor combined
Console.WriteLine("\n═══ Combined Demo ═══");
Console.WriteLine("  Relay ON → Motor 50%...");
await relay.OnAsync();
await motor.SetSpeedAsync(50);
await Task.Delay(2000);

Console.WriteLine("  Stopping all...");
await motor.BrakeAsync();
await relay.OffAsync();

Console.WriteLine("\n✓ Demo complete!");
