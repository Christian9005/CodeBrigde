using CodeBridge.Core.Enums;

namespace CodeBridge.Core.Abstractions;

/// <summary>
/// Provides GPIO interrupt (edge detection) capabilities.
/// Allows attaching callbacks to GPIO pins that trigger on rising/falling/change edges.
/// </summary>
public interface IGpioInterruptController : IDisposable
{
    /// <summary>
    /// Attaches an interrupt handler to a GPIO pin.
    /// </summary>
    /// <param name="pin">GPIO pin number.</param>
    /// <param name="edge">Edge trigger type.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AttachInterruptAsync(int pin, InterruptEdge edge, CancellationToken ct = default);

    /// <summary>
    /// Detaches the interrupt from a GPIO pin.
    /// </summary>
    Task DetachInterruptAsync(int pin, CancellationToken ct = default);

    /// <summary>
    /// Polls for triggered interrupts. Returns events since last poll.
    /// </summary>
    /// <returns>List of interrupt events (pin, count, edge).</returns>
    Task<IReadOnlyList<InterruptEvent>> PollAsync(CancellationToken ct = default);

    /// <summary>
    /// Continuously polls for interrupt events at the specified interval.
    /// </summary>
    IAsyncEnumerable<InterruptEvent> WatchAsync(int pin, TimeSpan pollInterval, CancellationToken ct = default);
}

/// <summary>
/// Represents a single interrupt event from a GPIO pin.
/// </summary>
public readonly record struct InterruptEvent(int Pin, uint Count, InterruptEdge Edge);

/// <summary>
/// Provides watchdog timer functionality.
/// The watchdog resets the MCU if not "fed" within the timeout period.
/// </summary>
public interface IWatchdogController
{
    /// <summary>Whether the watchdog is currently enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables the watchdog timer with the specified timeout.
    /// If the watchdog is not fed within this period, the MCU resets.
    /// </summary>
    /// <param name="timeout">Timeout period (1-120 seconds).</param>
    Task EnableAsync(TimeSpan timeout, CancellationToken ct = default);

    /// <summary>
    /// Feeds (resets) the watchdog timer, preventing a reset.
    /// Must be called periodically within the timeout window.
    /// </summary>
    Task FeedAsync(CancellationToken ct = default);

    /// <summary>
    /// Disables the watchdog timer.
    /// </summary>
    Task DisableAsync(CancellationToken ct = default);
}

/// <summary>
/// Provides deep sleep and power management capabilities.
/// </summary>
public interface IPowerController
{
    /// <summary>
    /// Puts the MCU into deep sleep for the specified duration.
    /// The board will reset and run setup() again after waking.
    /// </summary>
    /// <param name="duration">Sleep duration (1 second to 24 hours).</param>
    Task DeepSleepAsync(TimeSpan duration, CancellationToken ct = default);

    /// <summary>
    /// Puts the MCU into deep sleep until a GPIO pin reaches the specified level.
    /// Only RTC GPIOs can be used as wake sources (0,2,4,12-15,25-27,32-39 on ESP32).
    /// </summary>
    /// <param name="wakePin">RTC-capable GPIO pin.</param>
    /// <param name="wakeOnHigh">True to wake on HIGH, false to wake on LOW.</param>
    Task DeepSleepUntilPinAsync(int wakePin, bool wakeOnHigh = true, CancellationToken ct = default);
}

/// <summary>
/// Provides Over-The-Air firmware update capabilities.
/// </summary>
public interface IOtaController
{
    /// <summary>
    /// Starts an OTA update from a firmware binary URL.
    /// The MCU will download, flash, and restart automatically.
    /// </summary>
    /// <param name="firmwareUrl">HTTP(S) URL to the firmware .bin file.</param>
    Task UpdateFromUrlAsync(string firmwareUrl, CancellationToken ct = default);

    /// <summary>
    /// Gets the current OTA update status and progress.
    /// </summary>
    /// <returns>Status string and progress percentage (0-100).</returns>
    Task<(string Status, int Progress)> GetStatusAsync(CancellationToken ct = default);
}

/// <summary>
/// Provides MQTT publish/subscribe messaging capabilities.
/// Requires WiFi connectivity.
/// </summary>
public interface IMqttClient : IDisposable
{
    /// <summary>Whether the MQTT client is currently connected to a broker.</summary>
    bool IsConnected { get; }

    /// <summary>The broker address this client is connected to.</summary>
    string? Broker { get; }

    /// <summary>
    /// Connects to an MQTT broker.
    /// </summary>
    /// <param name="broker">Broker hostname or IP address.</param>
    /// <param name="port">Broker port (default: 1883).</param>
    /// <param name="clientId">Client identifier (auto-generated if null).</param>
    Task ConnectAsync(string broker, int port = 1883, string? clientId = null, CancellationToken ct = default);

    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    Task PublishAsync(string topic, string message, CancellationToken ct = default);

    /// <summary>
    /// Subscribes to a topic to receive messages.
    /// </summary>
    Task SubscribeAsync(string topic, CancellationToken ct = default);

    /// <summary>
    /// Unsubscribes from a topic.
    /// </summary>
    Task UnsubscribeAsync(string topic, CancellationToken ct = default);

    /// <summary>
    /// Reads the next available message from subscribed topics.
    /// Returns null if no messages are available.
    /// </summary>
    Task<MqttMessage?> ReadMessageAsync(CancellationToken ct = default);

    /// <summary>
    /// Continuously reads messages from subscribed topics.
    /// </summary>
    IAsyncEnumerable<MqttMessage> ReadMessagesAsync(TimeSpan pollInterval, CancellationToken ct = default);

    /// <summary>
    /// Disconnects from the MQTT broker.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);
}

/// <summary>
/// Represents an MQTT message received from a subscribed topic.
/// </summary>
public record MqttMessage(string Topic, string Payload)
{
    public override string ToString() => $"[{Topic}] {Payload}";
}
