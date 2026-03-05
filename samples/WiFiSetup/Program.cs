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

Console.WriteLine($"\n📶 Configuring WiFi ({ssid})...");
Console.WriteLine("   (This may take up to 15 seconds...)");

// WiFi config needs longer timeout since ESP32 waits up to 10s to connect
if (board.Transport is CodeBridge.Transport.Serial.SerialTransport serial)
    serial.CommandTimeoutSeconds = 15;

var wifiResponse = await board.Transport.SendCommandAsync(
    BridgeProtocol.BuildCommand(BridgeProtocol.CMD_WIFI_CONFIG, ssid, password));

// Restore default timeout
if (board.Transport is CodeBridge.Transport.Serial.SerialTransport serial2)
    serial2.CommandTimeoutSeconds = 5;

var (success, data) = BridgeProtocol.ParseResponse(wifiResponse);

if (success)
{
    Console.WriteLine($"✅ WiFi connected! ESP32 address: {data}");
    
    // ── Step 3: Get WiFi status ─────────────────────────────
    var statusResponse = await board.Transport.SendCommandAsync(
        BridgeProtocol.BuildCommand(BridgeProtocol.CMD_WIFI_STATUS));
    var (statOk, statData) = BridgeProtocol.ParseResponse(statusResponse);
    if (statOk)
        Console.WriteLine($"   Status: {statData}");
    
    Console.WriteLine("\n🎉 WiFi is configured and saved to flash!");
    Console.WriteLine("   The ESP32 will auto-connect on next boot.");
    Console.WriteLine($"\n   To connect via WiFi from C#:");
    Console.WriteLine($"   .WiFi(\"{data.Split(':')[0]}\").ToESP32().BuildAsync()");
}
else
{
    Console.WriteLine($"❌ WiFi failed: {data}");
}
