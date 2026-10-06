using System.Globalization;
using System.Text.Json.Nodes;
using CodeBridge.Hosting;

namespace CodeBridge.HomeAssistant;

/// <summary>Publishes MQTT messages on behalf of an entity.</summary>
public interface IHomeAssistantPublisher
{
    Task PublishAsync(string topic, string payload, bool retain, CancellationToken ct);
}

/// <summary>Names and topics shared by every entity of one device.</summary>
public sealed class HomeAssistantContext
{
    public HomeAssistantContext(HomeAssistantOptions options)
    {
        DeviceId = HomeAssistantOptions.SanitizeId(options.DeviceId);
        DeviceName = options.DeviceName;
        DiscoveryPrefix = options.DiscoveryPrefix.Trim('/');
        BaseTopic = options.BaseTopic.Trim('/');
    }

    public string DeviceId { get; }
    public string DeviceName { get; }
    public string DiscoveryPrefix { get; }
    public string BaseTopic { get; }
    public string AvailabilityTopic => $"{BaseTopic}/{DeviceId}/availability";
    public string StatusTopic => $"{DiscoveryPrefix}/status";

    /// <summary>Home Assistant groups entities by this "device" block.</summary>
    public JsonObject DeviceBlock(string? firmware = null)
    {
        var device = new JsonObject
        {
            ["identifiers"] = new JsonArray(DeviceId),
            ["name"] = DeviceName,
            ["manufacturer"] = "CodeBridge",
            ["model"] = "CodeBridge board"
        };
        if (!string.IsNullOrEmpty(firmware))
            device["sw_version"] = firmware;
        return device;
    }
}

/// <summary>One thing Home Assistant sees: a switch, a light, a sensor...</summary>
public abstract class HomeAssistantEntity
{
    protected HomeAssistantEntity(string id, string name)
    {
        Id = HomeAssistantOptions.SanitizeId(id);
        Name = name;
    }

    /// <summary>Stable id (letters, digits, dashes) used in topics and as the unique id in Home Assistant.</summary>
    public string Id { get; }

    public string Name { get; }

    /// <summary>MQTT discovery component: <c>switch</c>, <c>light</c>, <c>sensor</c>, <c>binary_sensor</c>.</summary>
    public abstract string Component { get; }

    /// <summary>How often <see cref="PollAsync"/> runs, or null for entities that only react to commands.</summary>
    public virtual TimeSpan? PollInterval => null;

    public string ConfigTopic(HomeAssistantContext ctx) => $"{ctx.DiscoveryPrefix}/{Component}/{ctx.DeviceId}/{Id}/config";

    public string Topic(HomeAssistantContext ctx, string suffix = "") =>
        suffix.Length == 0 ? $"{ctx.BaseTopic}/{ctx.DeviceId}/{Id}" : $"{ctx.BaseTopic}/{ctx.DeviceId}/{Id}/{suffix}";

    /// <summary>The retained discovery message that makes the entity appear in Home Assistant.</summary>
    public JsonObject BuildConfig(HomeAssistantContext ctx, string? firmware)
    {
        var config = new JsonObject
        {
            ["name"] = Name,
            ["unique_id"] = $"{ctx.DeviceId}_{Id}",
            ["availability_topic"] = ctx.AvailabilityTopic,
            ["device"] = ctx.DeviceBlock(firmware)
        };
        Describe(ctx, config);
        return config;
    }

    protected abstract void Describe(HomeAssistantContext ctx, JsonObject config);

    public virtual IEnumerable<string> CommandTopics(HomeAssistantContext ctx) => Array.Empty<string>();

    /// <summary>Handles a command for this entity. Returns false when the topic is not one of its own.</summary>
    public virtual Task<bool> HandleCommandAsync(HomeAssistantContext ctx, string topic, string payload, BoardService board, IHomeAssistantPublisher publisher, CancellationToken ct) =>
        Task.FromResult(false);

    /// <summary>Runs once after the connection is up (put the board in a known state, publish the first state).</summary>
    public virtual Task InitializeAsync(HomeAssistantContext ctx, BoardService board, IHomeAssistantPublisher publisher, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Reads the board and publishes a new state when something changed.</summary>
    public virtual Task PollAsync(HomeAssistantContext ctx, BoardService board, IHomeAssistantPublisher publisher, DateTime utcNow, TimeSpan heartbeat, CancellationToken ct) =>
        Task.CompletedTask;

    /// <summary>Forget the last published values so the next poll publishes again (after Home Assistant or the broker restarted).</summary>
    public virtual void ResetState()
    {
    }

    protected static string On(bool value) => value ? "ON" : "OFF";

    protected static bool IsOn(string payload) => payload.Trim().Equals("ON", StringComparison.OrdinalIgnoreCase);
}

/// <summary>A digital output as a Home Assistant switch (relay, lamp, LED).</summary>
public sealed class SwitchEntity : HomeAssistantEntity
{
    private readonly int _pin;
    private readonly bool _activeLow;
    private readonly bool _initial;

    public SwitchEntity(string id, string name, int pin, bool activeLow = false, bool initialOn = false)
        : base(id, name)
    {
        _pin = pin;
        _activeLow = activeLow;
        _initial = initialOn;
    }

    public override string Component => "switch";

    protected override void Describe(HomeAssistantContext ctx, JsonObject config)
    {
        config["command_topic"] = Topic(ctx, "set");
        config["state_topic"] = Topic(ctx, "state");
        config["payload_on"] = "ON";
        config["payload_off"] = "OFF";
    }

    public override IEnumerable<string> CommandTopics(HomeAssistantContext ctx) => new[] { Topic(ctx, "set") };

    public override async Task InitializeAsync(HomeAssistantContext ctx, BoardService board, IHomeAssistantPublisher publisher, CancellationToken ct)
    {
        await Apply(board, _initial, ct);
        await publisher.PublishAsync(Topic(ctx, "state"), On(_initial), false, ct);
    }

    public override async Task<bool> HandleCommandAsync(HomeAssistantContext ctx, string topic, string payload, BoardService board, IHomeAssistantPublisher publisher, CancellationToken ct)
    {
        if (topic != Topic(ctx, "set"))
            return false;

        var on = IsOn(payload);
        await Apply(board, on, ct);
        await publisher.PublishAsync(Topic(ctx, "state"), On(on), false, ct);
        return true;
    }

    private Task Apply(BoardService board, bool on, CancellationToken ct) => board.WriteDigitalAsync(_pin, _activeLow ? !on : on, ct: ct);
}

/// <summary>A PWM output as a dimmable Home Assistant light.</summary>
public sealed class LightEntity : HomeAssistantEntity
{
    private readonly int _pin;
    private readonly int _frequency;
    private int _brightness = 255;
    private bool _on;

    public LightEntity(string id, string name, int pin, int frequencyHz = 5000) : base(id, name)
    {
        _pin = pin;
        _frequency = frequencyHz;
    }

    public override string Component => "light";

    protected override void Describe(HomeAssistantContext ctx, JsonObject config)
    {
        config["command_topic"] = Topic(ctx, "set");
        config["state_topic"] = Topic(ctx, "state");
        config["brightness_command_topic"] = Topic(ctx, "brightness/set");
        config["brightness_state_topic"] = Topic(ctx, "brightness");
        config["brightness_scale"] = 255;
        config["payload_on"] = "ON";
        config["payload_off"] = "OFF";
    }

    public override IEnumerable<string> CommandTopics(HomeAssistantContext ctx) => new[] { Topic(ctx, "set"), Topic(ctx, "brightness/set") };

    public override async Task InitializeAsync(HomeAssistantContext ctx, BoardService board, IHomeAssistantPublisher publisher, CancellationToken ct)
    {
        _on = false;
        await board.WritePwmAsync(_pin, 0, _frequency, ct);
        await publisher.PublishAsync(Topic(ctx, "state"), "OFF", false, ct);
        await publisher.PublishAsync(Topic(ctx, "brightness"), _brightness.ToString(CultureInfo.InvariantCulture), false, ct);
    }

    public override async Task<bool> HandleCommandAsync(HomeAssistantContext ctx, string topic, string payload, BoardService board, IHomeAssistantPublisher publisher, CancellationToken ct)
    {
        if (topic == Topic(ctx, "brightness/set"))
        {
            if (!int.TryParse(payload.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return true; // ignore garbage but acknowledge the topic as ours

            _brightness = Math.Clamp(value, 0, 255);
            _on = _brightness > 0;
            await board.WritePwmAsync(_pin, _brightness, _frequency, ct);
            await publisher.PublishAsync(Topic(ctx, "brightness"), _brightness.ToString(CultureInfo.InvariantCulture), false, ct);
            await publisher.PublishAsync(Topic(ctx, "state"), On(_on), false, ct);
            return true;
        }

        if (topic == Topic(ctx, "set"))
        {
            _on = IsOn(payload);
            if (_on && _brightness == 0)
                _brightness = 255;

            await board.WritePwmAsync(_pin, _on ? _brightness : 0, _frequency, ct);
            await publisher.PublishAsync(Topic(ctx, "state"), On(_on), false, ct);
            await publisher.PublishAsync(Topic(ctx, "brightness"), _brightness.ToString(CultureInfo.InvariantCulture), false, ct);
            return true;
        }

        return false;
    }
}

/// <summary>A number read from the board (analog pin or any custom reading) as a Home Assistant sensor.</summary>
public sealed class SensorEntity : HomeAssistantEntity
{
    private readonly Func<BoardService, CancellationToken, Task<double>> _read;
    private readonly TimeSpan _interval;
    private readonly double _deadband;
    private readonly int _decimals;
    private string? _lastPayload;
    private double _lastValue = double.NaN;
    private DateTime _lastPublish = DateTime.MinValue;

    /// <param name="read">Returns the value to publish, already scaled to <paramref name="unit"/>.</param>
    /// <param name="interval">Seconds between reads.</param>
    /// <param name="deadband">Smallest change worth publishing.</param>
    public SensorEntity(string id, string name, Func<BoardService, CancellationToken, Task<double>> read,
        string? unit = null, string? deviceClass = null, TimeSpan? interval = null, double deadband = 0, int decimals = 1)
        : base(id, name)
    {
        _read = read;
        Unit = unit;
        DeviceClass = deviceClass;
        _interval = interval ?? TimeSpan.FromSeconds(5);
        _deadband = Math.Max(0, deadband);
        _decimals = Math.Clamp(decimals, 0, 6);
    }

    public string? Unit { get; }
    public string? DeviceClass { get; }

    public override string Component => "sensor";

    public override TimeSpan? PollInterval => _interval;

    protected override void Describe(HomeAssistantContext ctx, JsonObject config)
    {
        config["state_topic"] = Topic(ctx, "state");
        config["state_class"] = "measurement";
        if (!string.IsNullOrEmpty(Unit)) config["unit_of_measurement"] = Unit;
        if (!string.IsNullOrEmpty(DeviceClass)) config["device_class"] = DeviceClass;
    }

    public override void ResetState()
    {
        _lastPayload = null;
        _lastValue = double.NaN;
        _lastPublish = DateTime.MinValue;
    }

    public override async Task PollAsync(HomeAssistantContext ctx, BoardService board, IHomeAssistantPublisher publisher, DateTime utcNow, TimeSpan heartbeat, CancellationToken ct)
    {
        var value = Math.Round(await _read(board, ct), _decimals);
        var changed = double.IsNaN(_lastValue) || Math.Abs(value - _lastValue) > _deadband || (_deadband == 0 && value != _lastValue);
        var stale = utcNow - _lastPublish >= heartbeat;
        if (!changed && !stale)
            return;

        var payload = value.ToString("0.######", CultureInfo.InvariantCulture);
        _lastValue = value;
        _lastPublish = utcNow;
        if (payload == _lastPayload && !stale)
            return;

        _lastPayload = payload;
        await publisher.PublishAsync(Topic(ctx, "state"), payload, false, ct);
    }
}

/// <summary>An on/off input (button, door contact, motion sensor) as a Home Assistant binary sensor.</summary>
public sealed class BinarySensorEntity : HomeAssistantEntity
{
    private readonly Func<BoardService, CancellationToken, Task<bool>> _read;
    private readonly TimeSpan _interval;
    private bool? _last;
    private DateTime _lastPublish = DateTime.MinValue;

    public BinarySensorEntity(string id, string name, Func<BoardService, CancellationToken, Task<bool>> read, string? deviceClass = null, TimeSpan? interval = null)
        : base(id, name)
    {
        _read = read;
        DeviceClass = deviceClass;
        _interval = interval ?? TimeSpan.FromSeconds(1);
    }

    public string? DeviceClass { get; }

    public override string Component => "binary_sensor";

    public override TimeSpan? PollInterval => _interval;

    protected override void Describe(HomeAssistantContext ctx, JsonObject config)
    {
        config["state_topic"] = Topic(ctx, "state");
        config["payload_on"] = "ON";
        config["payload_off"] = "OFF";
        if (!string.IsNullOrEmpty(DeviceClass)) config["device_class"] = DeviceClass;
    }

    public override void ResetState()
    {
        _last = null;
        _lastPublish = DateTime.MinValue;
    }

    public override async Task PollAsync(HomeAssistantContext ctx, BoardService board, IHomeAssistantPublisher publisher, DateTime utcNow, TimeSpan heartbeat, CancellationToken ct)
    {
        var value = await _read(board, ct);
        if (_last == value && utcNow - _lastPublish < heartbeat)
            return;

        _last = value;
        _lastPublish = utcNow;
        await publisher.PublishAsync(Topic(ctx, "state"), On(value), false, ct);
    }
}
