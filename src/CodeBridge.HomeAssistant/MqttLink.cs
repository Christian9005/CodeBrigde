using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace CodeBridge.HomeAssistant;

/// <summary>The few MQTT operations the bridge needs. Tests replace it with an in-memory fake.</summary>
public interface IMqttLink : IAsyncDisposable
{
    bool IsConnected { get; }

    /// <summary>Raised for every message received on a subscribed topic (topic, payload as text).</summary>
    event Func<string, string, Task>? MessageReceived;

    /// <summary>Raised when the connection to the broker is lost.</summary>
    event Action? Disconnected;

    /// <param name="willTopic">Published by the broker (retained) when this client disappears without saying goodbye.</param>
    Task ConnectAsync(HomeAssistantOptions options, string clientId, string willTopic, string willPayload, CancellationToken ct);

    Task SubscribeAsync(string topic, CancellationToken ct);

    Task PublishAsync(string topic, string payload, bool retain, CancellationToken ct);

    Task DisconnectAsync(CancellationToken ct);
}

/// <summary>MQTT over MQTTnet.</summary>
public sealed class MqttNetLink : IMqttLink
{
    private readonly IMqttClient _client = new MqttFactory().CreateMqttClient();

    public MqttNetLink()
    {
        _client.ApplicationMessageReceivedAsync += async e =>
        {
            var handler = MessageReceived;
            if (handler is null)
                return;

            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            await handler(e.ApplicationMessage.Topic, payload);
        };
        _client.DisconnectedAsync += _ =>
        {
            Disconnected?.Invoke();
            return Task.CompletedTask;
        };
    }

    public bool IsConnected => _client.IsConnected;

    public event Func<string, string, Task>? MessageReceived;

    public event Action? Disconnected;

    public async Task ConnectAsync(HomeAssistantOptions options, string clientId, string willTopic, string willPayload, CancellationToken ct)
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(options.Broker, options.Port)
            .WithClientId(clientId)
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .WithTimeout(TimeSpan.FromSeconds(10))
            .WithWillTopic(willTopic)
            .WithWillPayload(willPayload)
            .WithWillRetain(true)
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

        if (!string.IsNullOrEmpty(options.Username))
            builder.WithCredentials(options.Username, options.Password);
        if (options.UseTls)
            builder.WithTlsOptions(o => o.UseTls());

        await _client.ConnectAsync(builder.Build(), ct);
    }

    public async Task SubscribeAsync(string topic, CancellationToken ct) =>
        await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithAtLeastOnceQoS().Build(), ct);

    public async Task PublishAsync(string topic, string payload, bool retain, CancellationToken ct) =>
        await _client.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build(), ct);

    public async Task DisconnectAsync(CancellationToken ct)
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build(), ct);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
