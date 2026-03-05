// ═══════════════════════════════════════════════════════════════
//  CodeBridge Sample: Blink LED
//  ────────────────────────────
//  This shows how a full-stack C# developer can control
//  an ESP32's LED pin — no C/C++ knowledge required!
//
//  Hardware: Connect an LED to GPIO 2 (built-in on most ESP32 boards)
//  Firmware: Flash the CodeBridge firmware to your ESP32 first
// ═══════════════════════════════════════════════════════════════

using CodeBridge.Core;
using CodeBridge.Core.Enums;
using CodeBridge.ESP32;
using CodeBridge.Transport;

// ── Step 1: Discover available boards ────────────────────────
Console.WriteLine("CodeBridge - Blink LED Sample");
Console.WriteLine("────────────────────────────────");

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

// ── Step 2: Connect to the ESP32 ─────────────────────────────
Console.WriteLine($"\nConnecting to ESP32 on {selectedPort}...");

await using var board = await CodeBridgeBuilder
    .Connect()
    .Serial(selectedPort)
    .ToESP32()
    .BuildAsync();

var info = await board.GetInfoAsync();
Console.WriteLine($"Connected! {info.ChipModel} @ {info.CpuFrequencyMHz}MHz");
Console.WriteLine($"   Free heap: {info.FreeHeapBytes:N0} bytes");
Console.WriteLine($"   Firmware: v{board.FirmwareVersion}");

// ── Step 3: Blink the LED! ───────────────────────────────────
const int LED_PIN = 2; // Built-in LED on most ESP32 boards

await board.Gpio.SetPinModeAsync(LED_PIN, PinMode.Output);
Console.WriteLine($"\nBlinking LED on GPIO {LED_PIN}... Press Ctrl+C to stop.\n");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    int count = 0;
    while (!cts.Token.IsCancellationRequested)
    {
        await board.Gpio.DigitalWriteAsync(LED_PIN, PinValue.High);
        Console.Write($"\r  LED ON  (blink #{++count})");
        await Task.Delay(500, cts.Token);

        await board.Gpio.DigitalWriteAsync(LED_PIN, PinValue.Low);
        Console.Write($"\r  LED OFF (blink #{count})  ");
        await Task.Delay(500, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Clean exit
}

await board.Gpio.DigitalWriteAsync(LED_PIN, PinValue.Low);
Console.WriteLine("\n\nDone! LED turned off.");
