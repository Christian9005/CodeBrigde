using CodeBridge.ESP32;
using CodeBridge.Core.Tests.Mocks;
using CodeBridge.Core.Abstractions;
using Xunit;

namespace CodeBridge.Core.Tests.ESP32;

public class ESP32MqttClientTests
{
    // ── Connect ──────────────────────────────────────────────

    [Fact]
    public async Task ConnectAsync_Sends_MQC_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:CONNECTED");
        var mqtt = new ESP32MqttClient(transport);

        await mqtt.ConnectAsync("broker.local", 1883, "client1");

        Assert.Equal("MQC:broker.local:1883:client1\n", transport.LastCommand);
        Assert.True(mqtt.IsConnected);
        Assert.Equal("broker.local", mqtt.Broker);
    }

    [Fact]
    public async Task ConnectAsync_Throws_On_Empty_Broker()
    {
        var transport = new MockTransport();
        var mqtt = new ESP32MqttClient(transport);

        await Assert.ThrowsAsync<ArgumentException>(
            () => mqtt.ConnectAsync("", 1883, "client1"));
    }

    [Fact]
    public async Task ConnectAsync_Throws_On_Error()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("ERR:Connection refused");
        var mqtt = new ESP32MqttClient(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mqtt.ConnectAsync("invalid.host", 1883, "client1"));
    }

    // ── Publish ──────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_Sends_MQP_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:CONNECTED");
        transport.EnqueueResponse("OK:PUBLISHED");
        var mqtt = new ESP32MqttClient(transport);
        await mqtt.ConnectAsync("broker.local", 1883, "client1");

        await mqtt.PublishAsync("test/topic", "Hello MQTT");

        Assert.Equal("MQP:test/topic:Hello MQTT\n", transport.LastCommand);
    }

    [Fact]
    public async Task PublishAsync_Throws_When_Not_Connected()
    {
        var transport = new MockTransport();
        var mqtt = new ESP32MqttClient(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mqtt.PublishAsync("test/topic", "Hello"));
    }

    // ── Subscribe ────────────────────────────────────────────

    [Fact]
    public async Task SubscribeAsync_Sends_MQS_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:CONNECTED");
        transport.EnqueueResponse("OK:SUBSCRIBED");
        var mqtt = new ESP32MqttClient(transport);
        await mqtt.ConnectAsync("broker.local", 1883, "client1");

        await mqtt.SubscribeAsync("sensor/temp");

        Assert.Equal("MQS:sensor/temp\n", transport.LastCommand);
    }

    [Fact]
    public async Task SubscribeAsync_Throws_When_Not_Connected()
    {
        var transport = new MockTransport();
        var mqtt = new ESP32MqttClient(transport);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mqtt.SubscribeAsync("test/topic"));
    }

    // ── Unsubscribe ──────────────────────────────────────────

    [Fact]
    public async Task UnsubscribeAsync_Sends_MQU_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:CONNECTED");
        transport.EnqueueResponse("OK:UNSUBSCRIBED");
        var mqtt = new ESP32MqttClient(transport);
        await mqtt.ConnectAsync("broker.local", 1883, "client1");

        await mqtt.UnsubscribeAsync("sensor/temp");

        Assert.Equal("MQU:sensor/temp\n", transport.LastCommand);
    }

    // ── ReadMessage ──────────────────────────────────────────

    [Fact]
    public async Task ReadMessageAsync_Parses_Message()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:CONNECTED");
        transport.EnqueueResponse("OK:sensor/temp|25.6");
        var mqtt = new ESP32MqttClient(transport);
        await mqtt.ConnectAsync("broker.local", 1883, "client1");

        var msg = await mqtt.ReadMessageAsync();

        Assert.NotNull(msg);
        Assert.Equal("sensor/temp", msg!.Topic);
        Assert.Equal("25.6", msg.Payload);
        Assert.StartsWith("MQR", transport.LastCommand);
    }

    [Fact]
    public async Task ReadMessageAsync_Returns_Null_When_No_Message()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:CONNECTED");
        transport.EnqueueResponse("OK:NONE");
        var mqtt = new ESP32MqttClient(transport);
        await mqtt.ConnectAsync("broker.local", 1883, "client1");

        var msg = await mqtt.ReadMessageAsync();

        Assert.Null(msg);
    }

    // ── Disconnect ───────────────────────────────────────────

    [Fact]
    public async Task DisconnectAsync_Sends_MQD_Command()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:CONNECTED");
        transport.EnqueueResponse("OK:DISCONNECTED");
        var mqtt = new ESP32MqttClient(transport);
        await mqtt.ConnectAsync("broker.local", 1883, "client1");

        await mqtt.DisconnectAsync();

        Assert.Equal("MQD\n", transport.LastCommand);
        Assert.False(mqtt.IsConnected);
        Assert.Null(mqtt.Broker);
    }

    [Fact]
    public async Task DisconnectAsync_Noop_When_Not_Connected()
    {
        var transport = new MockTransport();
        var mqtt = new ESP32MqttClient(transport);

        // Should not throw or send any command
        await mqtt.DisconnectAsync();

        Assert.Empty(transport.SentCommands);
    }

    // ── Dispose ──────────────────────────────────────────────

    [Fact]
    public async Task Dispose_Disconnects_If_Connected()
    {
        var transport = new MockTransport();
        transport.EnqueueResponse("OK:CONNECTED");
        transport.EnqueueResponse("OK:DISCONNECTED");
        var mqtt = new ESP32MqttClient(transport);
        await mqtt.ConnectAsync("broker.local", 1883, "client1");

        mqtt.Dispose();

        Assert.False(mqtt.IsConnected);
    }
}
