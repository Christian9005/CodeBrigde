// CodeBridge in a console app: read a sensor every second until Ctrl+C.
//   dotnet add package CodeBridge.ESP32
//   dotnet run -- COM3          (or omit the port to use the first board found)
using CodeBridge.Core;
using CodeBridge.ESP32;
using CodeBridge.Transport;

var port = args.FirstOrDefault() ?? BoardDiscovery.DiscoverPorts().FirstOrDefault()?.Name;
if (port is null)
{
    Console.WriteLine("No serial port found. Plug in your ESP32 (with the CodeBridge firmware) or pass the port: dotnet run -- COM3");
    return 1;
}

using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); };

Console.WriteLine($"Connecting to {port}...");
await using var board = await CodeBridgeBuilder.Connect().Serial(port).ToESP32().BuildAsync(cancel.Token);
Console.WriteLine($"Connected. Firmware {board.FirmwareVersion}. Reading GPIO 34, press Ctrl+C to stop.");

while (!cancel.IsCancellationRequested)
{
    var raw = await board.Gpio.AnalogReadAsync(34, cancel.Token);
    Console.WriteLine($"{DateTime.Now:HH:mm:ss}  GPIO 34 = {raw,4}  ({raw / 4095.0 * 3.3:0.00} V)");

    try { await Task.Delay(1000, cancel.Token); }
    catch (OperationCanceledException) { }
}

return 0;
