// ═══════════════════════════════════════════════════════════════
//  CodeBridge Sample: WiFi Blink LED
//  ──────────────────────────────────
//  Same as BlinkLed, but over WiFi instead of USB cable!
//  
//  Setup:
//    1. Flash the v0.2.0 firmware to your ESP32 via USB
//    2. Configure WiFi via serial: the firmware saves credentials
//    3. Note the IP address shown in the serial monitor
//    4. Run this sample with the IP address — no cable needed!
//
//  First time? Use the serial sample to configure WiFi:
//    - Connect via serial, the firmware will print the IP address
//    - Or send WCFG:YourSSID:YourPassword via serial monitor
// ═══════════════════════════════════════════════════════════════

using CodeBridge.Core;
using CodeBridge.Core.Enums;
using CodeBridge.ESP32;

// ── Step 1: Get ESP32 IP address ─────────────────────────────
Console.WriteLine("CodeBridge - WiFi Blink LED Sample");
Console.WriteLine("──────────────────────────────────────");

string? ipAddress;
int port;

if (args.Length >= 1)
{
    ipAddress = args[0];
    port = args.Length >= 2 ? int.Parse(args[1]) : 8080;
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
    .WiFi(ipAddress, port)
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
