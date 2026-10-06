using System.Net;
using System.Net.Sockets;
using System.Text;
using CodeBridge.HomeAssistant;
using CodeBridge.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;

namespace CodeBridge.HomeAssistant.Tests;

/// <summary>The bridge against a real MQTT broker (MQTTnet's embedded one) over TCP, the way Home Assistant's Mosquitto behaves.</summary>
public class RealBrokerTests
{
    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact]
    public async Task Entities_appear_through_a_real_broker_and_obey_commands()
    {
        var port = FreePort();
        var factory = new MqttFactory();
        using var broker = factory.CreateMqttServer(new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(port).Build());
        await broker.StartAsync();

        // "Home Assistant": watches everything the board publishes
        var seen = new List<(string Topic, string Payload, bool Retain)>();
        using var observer = factory.CreateMqttClient();
        observer.ApplicationMessageReceivedAsync += e =>
        {
            lock (seen) seen.Add((e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment), e.ApplicationMessage.Retain));
            return Task.CompletedTask;
        };
        await observer.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", port).Build());
        await observer.SubscribeAsync("homeassistant/#");
        await observer.SubscribeAsync("codebridge/#");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCodeBridge(o => o.Port = "simulator");
        services.AddCodeBridgeHomeAssistant(
            o => { o.Broker = "127.0.0.1"; o.Port = port; o.DeviceId = "bench"; },
            ha => ha.Switch("led", "LED", pin: 2).AnalogSensor("light", "Light", pin: 34, intervalSeconds: 0.05, decimals: 0));
        await using var provider = services.BuildServiceProvider();
        var board = provider.GetRequiredService<BoardService>();
        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(CancellationToken.None);

        await WaitFor(() => Has(seen, "homeassistant/switch/bench/led/config") && Has(seen, "codebridge/bench/availability", "online"));

        // a client that connects later still receives the retained discovery message
        using var late = factory.CreateMqttClient();
        var lateSeen = new List<string>();
        late.ApplicationMessageReceivedAsync += e => { lock (lateSeen) lateSeen.Add(e.ApplicationMessage.Topic); return Task.CompletedTask; };
        await late.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", port).Build());
        await late.SubscribeAsync("homeassistant/+/bench/+/config");
        await WaitFor(() => { lock (lateSeen) return lateSeen.Count >= 2; });

        // the user flips the switch in Home Assistant
        await observer.PublishStringAsync("codebridge/bench/led/set", "ON");
        await WaitFor(() => board.Simulator!.GetOutput(2));
        await WaitFor(() => Has(seen, "codebridge/bench/led/state", "ON"));

        // the sensor streams
        board.Simulator!.SetAnalogInput(34, 1000);
        await WaitFor(() => Has(seen, "codebridge/bench/light/state", "1000"));

        foreach (var hosted in provider.GetServices<IHostedService>().Reverse())
            await hosted.StopAsync(CancellationToken.None);
        await WaitFor(() => Has(seen, "codebridge/bench/availability", "offline"));
        await broker.StopAsync();
    }

    [Fact]
    public async Task The_bridge_waits_for_a_broker_that_starts_late()
    {
        var port = FreePort();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCodeBridge(o => o.Port = "simulator");
        services.AddCodeBridgeHomeAssistant(
            o => { o.Broker = "127.0.0.1"; o.Port = port; o.DeviceId = "late"; o.ReconnectDelay = TimeSpan.FromMilliseconds(100); o.MaxReconnectDelay = TimeSpan.FromMilliseconds(200); },
            ha => ha.Switch("led", "LED", pin: 2));
        await using var provider = services.BuildServiceProvider();
        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(CancellationToken.None); // no broker yet: this must not throw or block

        await Task.Delay(400);

        var factory = new MqttFactory();
        using var broker = factory.CreateMqttServer(new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(port).Build());
        var announced = new TaskCompletionSource<bool>();
        broker.InterceptingPublishAsync += e =>
        {
            if (e.ApplicationMessage.Topic == "homeassistant/switch/late/led/config")
                announced.TrySetResult(true);
            return Task.CompletedTask;
        };
        await broker.StartAsync();

        Assert.True(await Task.WhenAny(announced.Task, Task.Delay(8000)) == announced.Task, "The bridge never connected to the late broker.");

        foreach (var hosted in provider.GetServices<IHostedService>().Reverse())
            await hosted.StopAsync(CancellationToken.None);
        await broker.StopAsync();
    }

    private static bool Has(List<(string Topic, string Payload, bool Retain)> seen, string topic, string? payload = null)
    {
        lock (seen) return seen.Any(m => m.Topic == topic && (payload is null || m.Payload == payload));
    }

    private static async Task WaitFor(Func<bool> condition, int seconds = 8)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < until, "The condition was not reached in time.");
            await Task.Delay(25);
        }
    }
}
