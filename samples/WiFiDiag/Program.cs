// Quick diagnostic tool - connects via serial and checks WiFi status
using CodeBridge.Core;
using CodeBridge.Core.Protocol;
using CodeBridge.ESP32;
using CodeBridge.Transport;

Console.WriteLine("CodeBridge - WiFi Diagnostics\n");

var ports = BoardDiscovery.DiscoverPorts();
var selectedPort = ports.FirstOrDefault(p => p.Name == "COM3")?.Name ?? ports[0].Name;

Console.WriteLine($"Connecting to {selectedPort}...");
await using var board = await CodeBridgeBuilder
    .Connect()
    .Serial(selectedPort)
    .ToESP32()
    .BuildAsync();

Console.WriteLine($"Connected! Firmware v{board.FirmwareVersion}\n");

// Check WiFi status
Console.WriteLine("── WiFi Status ──");
var statusResp = await board.Transport.SendCommandAsync(
    BridgeProtocol.BuildCommand(BridgeProtocol.CMD_WIFI_STATUS));
var (ok, data) = BridgeProtocol.ParseResponse(statusResp);
Console.WriteLine($"   Response: {(ok ? "OK" : "ERR")}: {data}");

// Try TCP port check
Console.WriteLine("\n── TCP Test ──");
try
{
    var tcpTest = new System.Net.Sockets.TcpClient();
    await tcpTest.ConnectAsync("192.168.68.51", 8080);
    Console.WriteLine("   TCP:8080 = OPEN ✅");
    tcpTest.Close();
}
catch (Exception ex)
{
    Console.WriteLine($"   TCP:8080 = CLOSED ❌ ({ex.Message})");
}
