// ═══════════════════════════════════════════════════════════════
//  CodeBridge Sample: WiFi Blink LED
//  ──────────────────────────────────
//  Same as BlinkLed, but over WiFi instead of USB cable!
//  
//  Setup (once, with the USB cable):
//    1. Flash the CodeBridge firmware (0.9+) to your ESP32.
//    2. Run the WiFiSetup sample: it saves your network on the board and prints
//       the board's IP address and a private pairing token.
//    3. Run this sample with the IP address and the token - no cable needed:
//         dotnet run -- 192.168.1.50 <token>
//       (or set the CODEBRIDGE_TOKEN environment variable instead of passing it).
//
//  The token is what keeps other devices on your network from controlling the board.
// ═══════════════════════════════════════════════════════════════

using CodeBridge.Core;
using CodeBridge.Core.Enums;
using CodeBridge.ESP32;

// ── Step 1: Get ESP32 IP address ─────────────────────────────
Console.WriteLine("CodeBridge - WiFi Blink LED Sample");
Console.WriteLine("──────────────────────────────────────");

string? ipAddress;
int port;
var token = Environment.GetEnvironmentVariable("CODEBRIDGE_TOKEN");

if (args.Length >= 1)
{
    ipAddress = args[0];
    port = 8080;
    if (args.Length >= 2)
        token = args[1];
}
else
{
    Console.Write("Enter ESP32 IP address (e.g. 192.168.1.100): ");
    ipAddress = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(ipAddress))
    {
        Console.WriteLine("IP address required. Connect via serial first to get it.");
        return;
    }

    Console.Write("Enter TCP port (8080): ");
    var portInput = Console.ReadLine();
    port = string.IsNullOrEmpty(portInput) ? 8080 : int.Parse(portInput);
}

// ── Step 2: Connect via WiFi ─────────────────────────────────
Console.WriteLine($"\nConnecting to ESP32 at {ipAddress}:{port} via WiFi...");

await using var board = await CodeBridgeBuilder
    .Connect()
    .WiFi(ipAddress, port, token)
    .ToESP32()
    .BuildAsync();

var info = await board.GetInfoAsync();
Console.WriteLine($"Connected! {info.ChipModel} @ {info.CpuFrequencyMHz}MHz");
Console.WriteLine($"   Free heap: {info.FreeHeapBytes:N0} bytes");
Console.WriteLine($"   Firmware: v{board.FirmwareVersion}");
Console.WriteLine($"   Transport: WiFi (TCP)");

// ── Step 3: Blink the LED! ───────────────────────────────────
const int LED_PIN = 2;

await board.Gpio.SetPinModeAsync(LED_PIN, PinMode.Output);
Console.WriteLine($"\nBlinking LED on GPIO {LED_PIN} over WiFi... Press Ctrl+C to stop.\n");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    int count = 0;
    while (!cts.Token.IsCancellationRequested)
    {
        await board.Gpio.DigitalWriteAsync(LED_PIN, PinValue.High);
        Console.Write($"\r  📡 LED ON  (blink #{++count})");
        await Task.Delay(500, cts.Token);

        await board.Gpio.DigitalWriteAsync(LED_PIN, PinValue.Low);
        Console.Write($"\r  📡 LED OFF (blink #{count})  ");
        await Task.Delay(500, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Clean exit
}

await board.Gpio.DigitalWriteAsync(LED_PIN, PinValue.Low);
Console.WriteLine("\n\nDone! LED turned off.");
