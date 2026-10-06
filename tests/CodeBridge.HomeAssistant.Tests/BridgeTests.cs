using System.Text.Json.Nodes;
using CodeBridge.HomeAssistant;
using CodeBridge.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CodeBridge.HomeAssistant.Tests;

/// <summary>An in-memory MQTT connection: records what the bridge publishes and lets a test play Home Assistant.</summary>
internal sealed class FakeMqttLink : IMqttLink
{
    public bool IsConnected { get; private set; }
    public List<(string Topic, string Payload, bool Retain)> Published { get; } = new();
    public List<string> Subscriptions { get; } = new();
    public (string Topic, string Payload)? Will { get; private set; }
    public event Func<string, string, Task>? MessageReceived;
    public event Action? Disconnected;

    public Task ConnectAsync(HomeAssistantOptions options, string clientId, string willTopic, string willPayload, CancellationToken ct)
    {
        IsConnected = true;
        Will = (willTopic, willPayload);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync(string topic, CancellationToken ct)
    {
        lock (Subscriptions) Subscriptions.Add(topic);
        return Task.CompletedTask;
    }

    public Task PublishAsync(string topic, string payload, bool retain, CancellationToken ct)
    {
        lock (Published) Published.Add((topic, payload, retain));
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task SendFromHomeAssistant(string topic, string payload) => MessageReceived?.Invoke(topic, payload) ?? Task.CompletedTask;

    public void DropConnection()
    {
        IsConnected = false;
        Disconnected?.Invoke();
    }

    public string? Last(string topic)
    {
        lock (Published) return Published.LastOrDefault(p => p.Topic == topic).Payload;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class BridgeTests
{
    private static HomeAssistantContext Context(string deviceId = "garage") => new(new HomeAssistantOptions { DeviceId = deviceId, DeviceName = "Garage board" });

    [Fact]
    public void Discovery_messages_follow_the_Home_Assistant_conventions()
    {
        var ctx = Context();
        var sw = new SwitchEntity("relay-1", "Relay", pin: 5);
        var config = sw.BuildConfig(ctx, "0.9.0");

        Assert.Equal("homeassistant/switch/garage/relay-1/config", sw.ConfigTopic(ctx));
        Assert.Equal("garage_relay-1", (string?)config["unique_id"]);
        Assert.Equal("codebridge/garage/relay-1/set", (string?)config["command_topic"]);
        Assert.Equal("codebridge/garage/availability", (string?)config["availability_topic"]);
        Assert.Equal("garage", (string?)config["device"]!["identifiers"]![0]);
        Assert.Equal("0.9.0", (string?)config["device"]!["sw_version"]);

        var light = new LightEntity("lamp", "Lamp", pin: 4).BuildConfig(ctx, null);
        Assert.Equal(255, (int?)light["brightness_scale"]);
        Assert.Equal("codebridge/garage/lamp/brightness/set", (string?)light["brightness_command_topic"]);

        var sensor = new SensorEntity("light", "Light", (_, _) => Task.FromResult(1.0), "%", "illuminance").BuildConfig(ctx, null);
        Assert.Equal("%", (string?)sensor["unit_of_measurement"]);
        Assert.Equal("illuminance", (string?)sensor["device_class"]);
        Assert.Equal("measurement", (string?)sensor["state_class"]);

        var binary = new BinarySensorEntity("door", "Door", (_, _) => Task.FromResult(true), "door");
        Assert.Equal("homeassistant/binary_sensor/garage/door/config", binary.ConfigTopic(ctx));
    }

    [Fact]
    public void Unsafe_ids_are_cleaned()
    {
        Assert.Equal("my-board-1", HomeAssistantOptions.SanitizeId("My Board #1"));
        Assert.Equal("board", HomeAssistantOptions.SanitizeId("###"));
    }

    private static (ServiceProvider Provider, List<FakeMqttLink> Links) Build(Action<HomeAssistantBuilder> entities)
    {
        var links = new List<FakeMqttLink>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCodeBridge(o => { o.Port = "simulator"; o.AutoConnect = true; });
        services.AddCodeBridgeHomeAssistant(
            o => { o.DeviceId = "test-board"; o.ReconnectDelay = TimeSpan.FromMilliseconds(20); o.MaxReconnectDelay = TimeSpan.FromMilliseconds(40); },
            entities);
        services.AddSingleton<Func<IMqttLink>>(() =>
        {
            var link = new FakeMqttLink();
            lock (links) links.Add(link);
            return link;
        });
        return (services.BuildServiceProvider(), links);
    }

    private static async Task Eventually(Func<bool> condition, int seconds = 5)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < until, "The condition was not reached in time.");
            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task The_bridge_announces_entities_executes_commands_and_publishes_sensor_values()
    {
        var (provider, links) = Build(ha => ha
            .Switch("led", "LED", pin: 2)
            .Light("lamp", "Lamp", pin: 4)
            .AnalogSensor("light", "Light", pin: 34, unit: "%", scale: 100.0 / 4095, intervalSeconds: 0.05)
            .BinarySensor("button", "Button", pin: 12, intervalSeconds: 0.05));
        await using var _ = provider;
        var board = provider.GetRequiredService<BoardService>();
        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(CancellationToken.None);

        await Eventually(() => links.Count > 0 && links[0].Last("codebridge/test-board/availability") == "online");
        var link = links[0];

        // retained discovery for all four entities, and the last will protects against a dirty disconnect
        foreach (var topic in new[] { "homeassistant/switch/test-board/led/config", "homeassistant/light/test-board/lamp/config", "homeassistant/sensor/test-board/light/config", "homeassistant/binary_sensor/test-board/button/config" })
        {
            var message = link.Published.First(p => p.Topic == topic);
            Assert.True(message.Retain);
            Assert.NotNull(JsonNode.Parse(message.Payload));
        }

        Assert.Equal(("codebridge/test-board/availability", "offline"), link.Will);
        Assert.Contains("codebridge/test-board/led/set", link.Subscriptions);
        Assert.Contains("homeassistant/status", link.Subscriptions);

        // Home Assistant switches the LED on and dims the lamp
        await link.SendFromHomeAssistant("codebridge/test-board/led/set", "ON");
        await Eventually(() => board.Simulator!.GetOutput(2));
        Assert.Equal("ON", link.Last("codebridge/test-board/led/state"));

        await link.SendFromHomeAssistant("codebridge/test-board/lamp/brightness/set", "128");
        await Eventually(() => board.Simulator!.GetPwmDuty(4) == 128);
        Assert.Equal("ON", link.Last("codebridge/test-board/lamp/state"));

        await link.SendFromHomeAssistant("codebridge/test-board/lamp/set", "OFF");
        await Eventually(() => board.Simulator!.GetPwmDuty(4) == 0);

        // the board's analog input arrives as a percentage, the button as ON/OFF
        board.Simulator!.SetAnalogInput(34, 2048);
        await Eventually(() => link.Last("codebridge/test-board/light/state") == "50");
        board.Simulator.SetDigitalInput(12, true);
        await Eventually(() => link.Last("codebridge/test-board/button/state") == "ON");

        foreach (var hosted in provider.GetServices<IHostedService>().Reverse())
            await hosted.StopAsync(CancellationToken.None);
        Assert.Equal("offline", link.Last("codebridge/test-board/availability"));
    }

    [Fact]
    public async Task Unchanged_sensor_values_are_not_republished_until_the_heartbeat()
    {
        var (provider, links) = Build(ha => ha.AnalogSensor("light", "Light", pin: 34, intervalSeconds: 0.02, decimals: 0));
        await using var _ = provider;
        var board = provider.GetRequiredService<BoardService>();
        await board.ReadAnalogAsync(34);
        board.Simulator!.SetAnalogInput(34, 700);
        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(CancellationToken.None);

        await Eventually(() => links.Count > 0 && links[0].Last("codebridge/test-board/light/state") == "700");
        await Task.Delay(300);

        Assert.Single(links[0].Published, p => p.Topic == "codebridge/test-board/light/state");
        foreach (var hosted in provider.GetServices<IHostedService>().Reverse())
            await hosted.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task When_Home_Assistant_restarts_the_entities_are_announced_again()
    {
        var (provider, links) = Build(ha => ha.Switch("led", "LED", pin: 2));
        await using var _ = provider;
        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(CancellationToken.None);
        await Eventually(() => links.Count > 0 && links[0].Last("codebridge/test-board/availability") == "online");
        var link = links[0];
        var before = link.Published.Count(p => p.Topic.EndsWith("/config"));

        await link.SendFromHomeAssistant("homeassistant/status", "online");

        await Eventually(() => link.Published.Count(p => p.Topic.EndsWith("/config")) > before);
        foreach (var hosted in provider.GetServices<IHostedService>().Reverse())
            await hosted.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task A_lost_broker_connection_is_re_established_with_fresh_announcements()
    {
        var (provider, links) = Build(ha => ha.Switch("led", "LED", pin: 2));
        await using var _ = provider;
        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(CancellationToken.None);
        await Eventually(() => links.Count > 0 && links[0].Last("codebridge/test-board/availability") == "online");

        links[0].DropConnection();

        await Eventually(() => links.Count >= 2 && links[1].Last("codebridge/test-board/availability") == "online");
        Assert.Contains(links[1].Published, p => p.Topic == "homeassistant/switch/test-board/led/config");
        foreach (var hosted in provider.GetServices<IHostedService>().Reverse())
            await hosted.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Duplicate_entity_ids_are_rejected_when_the_bridge_is_created()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCodeBridge(o => o.Port = "simulator");
        services.AddCodeBridgeHomeAssistant(_ => { }, ha => ha.Switch("same", "A", 2).Switch("same", "B", 4));
        await using var provider = services.BuildServiceProvider();

        var error = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<HomeAssistantBridge>());
        Assert.Contains("same", error.Message);
    }
}
