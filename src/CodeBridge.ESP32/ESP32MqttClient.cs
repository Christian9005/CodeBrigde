using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Protocol;
using System.Runtime.CompilerServices;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 MQTT client — publish/subscribe messaging via the firmware's PubSubClient.
/// Requires WiFi connectivity. Messages from subscribed topics are buffered
/// on the ESP32 (up to 32 messages) and retrieved via polling.
/// </summary>
public class ESP32MqttClient : IMqttClient
{
    private readonly ITransport _transport;
    private bool _disposed;

    public bool IsConnected { get; private set; }
    public string? Broker { get; private set; }

    public ESP32MqttClient(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task ConnectAsync(string broker, int port = 1883, string? clientId = null, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(broker))
            throw new ArgumentException("Broker address is required.", nameof(broker));

        var cmd = clientId != null
            ? BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MQTT_CONNECT, broker, port, clientId)
            : BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MQTT_CONNECT, broker, port);

        var response = await _transport.SendCommandAsync(cmd, ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"MQTT connect failed: {error}");
        IsConnected = true;
        Broker = broker;
    }

    public async Task PublishAsync(string topic, string message, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected) throw new InvalidOperationException("MQTT not connected.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MQTT_PUBLISH, topic, message), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"MQTT publish failed: {error}");
    }

    public async Task SubscribeAsync(string topic, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected) throw new InvalidOperationException("MQTT not connected.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MQTT_SUBSCRIBE, topic), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"MQTT subscribe failed: {error}");
    }

    public async Task UnsubscribeAsync(string topic, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected) throw new InvalidOperationException("MQTT not connected.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MQTT_UNSUBSCRIBE, topic), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"MQTT unsubscribe failed: {error}");
    }

    public async Task<MqttMessage?> ReadMessageAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected) throw new InvalidOperationException("MQTT not connected.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MQTT_READ), ct);
        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"MQTT read failed: {data}");

        if (data == "NONE") return null;

        // Format: "topic|payload"
        var sepIdx = data.IndexOf('|');
        if (sepIdx < 0) return new MqttMessage(data, "");
        var topic = data[..sepIdx];
        var payload = data[(sepIdx + 1)..];
        return new MqttMessage(topic, payload);
    }

    public async IAsyncEnumerable<MqttMessage> ReadMessagesAsync(
        TimeSpan pollInterval, [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var msg = await ReadMessageAsync(ct);
            if (msg != null)
                yield return msg;
            else
                await Task.Delay(pollInterval, ct);
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected) return;
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MQTT_DISCONNECT), ct);
        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success) throw new InvalidOperationException($"MQTT disconnect failed: {error}");
        IsConnected = false;
        Broker = null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (IsConnected)
            {
                try
                {
                    var response = _transport.SendCommandAsync(
                        BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MQTT_DISCONNECT)).GetAwaiter().GetResult();
                    IsConnected = false;
                    Broker = null;
                }
                catch { /* best effort */ }
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
