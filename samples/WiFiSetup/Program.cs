// ═══════════════════════════════════════════════════════════════
//  CodeBridge: WiFi Setup Helper
//  ─────────────────────────────
//  Connects via serial, configures WiFi on ESP32, 
//  then reconnects via WiFi to verify it works.
// ═══════════════════════════════════════════════════════════════

using CodeBridge.Core;
using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;
using CodeBridge.ESP32;
using CodeBridge.Transport;

Console.WriteLine("CodeBridge - WiFi Setup");
Console.WriteLine("═══════════════════════\n");

// ── Step 1: Connect via Serial ───────────────────────────────
var ports = BoardDiscovery.DiscoverPorts();
if (ports.Length == 0)
{
    Console.WriteLine("No serial ports found.");
    return;
}

Console.WriteLine("Available ports:");
for (int i = 0; i < ports.Length; i++)
    Console.WriteLine($"  [{i}] {ports[i].Name}");

// Auto-select COM3 or first available port
var selectedPort = ports.FirstOrDefault(p => p.Name == "COM3")?.Name ?? ports[0].Name;
Console.WriteLine($"\n📡 Connecting via Serial ({selectedPort})...");

await using var board = await CodeBridgeBuilder
    .Connect()
    .Serial(selectedPort)
    .ToESP32()
    .BuildAsync();

var info = await board.GetInfoAsync();
Console.WriteLine($"✅ Connected! {info.ChipModel} @ {info.CpuFrequencyMHz}MHz");
Console.WriteLine($"   Firmware: v{board.FirmwareVersion}");

// ── Step 2: Configure WiFi ──────────────────────────────────
var ssid = args.Length > 0 ? args[0] : "";
var password = args.Length > 1 ? args[1] : "";

if (string.IsNullOrEmpty(ssid))
{
    Console.Write("\nWiFi SSID: ");
    ssid = Console.ReadLine() ?? "";
}
if (string.IsNullOrEmpty(password))
{
    Console.Write("WiFi Password: ");
    password = Console.ReadLine() ?? "";
}

Console.WriteLine($"{Environment.NewLine}📶 Pairing the board and joining '{ssid}'...");
Console.WriteLine("   (This may take up to 30 seconds...)");

if (board.Transport is not CodeBridge.Transport.Serial.SerialTransport serial)
{
    Console.WriteLine("Wi-Fi setup needs the USB serial connection.");
    return;
}

try
{
    // Stores the network on the board and pairs it with a random access token. Provisioning is USB-only on purpose.
    var provisioner = new CodeBridge.Transport.Provisioning.Esp32WifiProvisioner(serial);
    var result = await provisioner.ProvisionAsync(ssid, password);

    Console.WriteLine($"✅ WiFi connected! ESP32 address: {result.IpAddress}:{result.Port}");
    Console.WriteLine();
    Console.WriteLine("🎉 WiFi is configured and saved to flash. The ESP32 reconnects on every boot.");
    Console.WriteLine();
    Console.WriteLine("   Keep this token secret: it is the only way to control the board over Wi-Fi.");
    Console.WriteLine($"   Token: {result.AccessToken}");
    Console.WriteLine();
    Console.WriteLine("   To connect via WiFi from C#:");
    Console.WriteLine($"   .WiFi(\"{result.IpAddress}\", {result.Port}, \"{result.AccessToken}\").ToESP32().BuildAsync()");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ WiFi failed: {ex.Message}");
}
