using System.Threading.Channels;
using CodeBridge.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeBridge.HomeAssistant;

/// <summary>
/// Keeps a CodeBridge board visible in Home Assistant: announces its entities with MQTT discovery, publishes their states,
/// executes commands from Home Assistant and survives broker or board restarts. Runs as a hosted service.
/// </summary>
public sealed class HomeAssistantBridge : BackgroundService, IHomeAssistantPublisher
{
    private readonly BoardService _board;
    private readonly HomeAssistantOptions _options;
    private readonly IReadOnlyList<HomeAssistantEntity> _entities;
    private readonly Func<IMqttLink> _linkFactory;
    private readonly ILogger<HomeAssistantBridge> _logger;
    private readonly HomeAssistantContext _context;
    private readonly Channel<(string Topic, string Payload)> _incoming =
        Channel.CreateBounded<(string, string)>(new BoundedChannelOptions(256) { FullMode = BoundedChannelFullMode.DropOldest });
    private readonly Dictionary<HomeAssistantEntity, DateTime> _nextPoll = new();
    private IMqttLink? _link;
    private string? _availability;

    public HomeAssistantBridge(
        BoardService board,
        IOptions<HomeAssistantOptions> options,
        IEnumerable<HomeAssistantEntity> entities,
        ILogger<HomeAssistantBridge> logger,
        Func<IMqttLink>? linkFactory = null)
    {
        _board = board;
        _options = options.Value;
        _entities = entities.ToList();
        _logger = logger;
        _linkFactory = linkFactory ?? (() => new MqttNetLink());
        _context = new HomeAssistantContext(_options);

        var duplicate = _entities.GroupBy(e => $"{e.Component}/{e.Id}").FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Two Home Assistant entities share the id '{duplicate.First().Id}'. Ids must be unique per type.");
    }

    public async Task PublishAsync(string topic, string payload, bool retain, CancellationToken ct)
    {
        var link = _link;
        if (link is { IsConnected: true })
            await link.PublishAsync(topic, payload, retain, ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = _options.ReconnectDelay;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(stoppingToken);
                delay = _options.ReconnectDelay;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Home Assistant link down ({Message}); retrying in {Delay:0}s.", ex.Message, delay.TotalSeconds);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, _options.MaxReconnectDelay.Ticks));
        }
    }

    private async Task RunSessionAsync(CancellationToken ct)
    {
        var link = _linkFactory();
        var lost = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        link.Disconnected += () => lost.TrySetResult();
        link.MessageReceived += (topic, payload) =>
        {
            _incoming.Writer.TryWrite((topic, payload));
            return Task.CompletedTask;
        };

        try
        {
            await link.ConnectAsync(_options, _context.DeviceId + "-" + Guid.NewGuid().ToString("N")[..6], _context.AvailabilityTopic, "offline", ct);
            _link = link;
            _availability = null;

            foreach (var topic in _entities.SelectMany(e => e.CommandTopics(_context)).Append(_context.StatusTopic))
                await link.SubscribeAsync(topic, ct);

            _logger.LogInformation("Connected to the Home Assistant broker at {Broker}:{Port} as device '{Device}' with {Count} entities.", _options.Broker, _options.Port, _context.DeviceId, _entities.Count);

            await AnnounceAsync(ct);
            foreach (var entity in _entities)
            {
                await SafeAsync(entity, () => entity.InitializeAsync(_context, _board, this, ct));
                _nextPoll[entity] = DateTime.UtcNow;
            }

            while (!ct.IsCancellationRequested && !lost.Task.IsCompleted)
            {
                await DrainCommandsAsync(ct);
                await UpdateAvailabilityAsync(ct);
                await PollAsync(ct);
                await Task.WhenAny(Task.Delay(200, ct), lost.Task);
            }

            if (lost.Task.IsCompleted && !ct.IsCancellationRequested)
                throw new IOException("The MQTT connection was lost.");
        }
        finally
        {
            _link = null;
            if (link.IsConnected)
            {
                try
                {
                    await link.PublishAsync(_context.AvailabilityTopic, "offline", true, CancellationToken.None);
                    await link.DisconnectAsync(CancellationToken.None);
                }
                catch (Exception)
                {
                    // the broker is gone; its last-will message covers us
                }
            }

            await link.DisposeAsync();
        }
    }

    /// <summary>Publishes the retained discovery config of every entity.</summary>
    private async Task AnnounceAsync(CancellationToken ct)
    {
        foreach (var entity in _entities)
        {
            var config = entity.BuildConfig(_context, _board.FirmwareVersion);
            await PublishAsync(entity.ConfigTopic(_context), config.ToJsonString(), true, ct);
            entity.ResetState();
        }
    }

    private async Task DrainCommandsAsync(CancellationToken ct)
    {
        while (_incoming.Reader.TryRead(out var message))
        {
            if (message.Topic == _context.StatusTopic)
            {
                // Home Assistant (re)started: it forgot the entities, announce them again.
                if (message.Payload.Trim().Equals("online", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Home Assistant came online; announcing the entities again.");
                    _availability = null;
                    await AnnounceAsync(ct);
                }

                continue;
            }

            foreach (var entity in _entities)
            {
                if (!entity.CommandTopics(_context).Contains(message.Topic))
                    continue;

                try
                {
                    await entity.HandleCommandAsync(_context, message.Topic, message.Payload, _board, this, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning("Command for '{Entity}' failed: {Message}", entity.Id, ex.Message);
                }
            }
        }
    }

    private async Task UpdateAvailabilityAsync(CancellationToken ct)
    {
        var state = _board.IsConnected ? "online" : "offline";
        if (state == _availability)
            return;

        _availability = state;
        await PublishAsync(_context.AvailabilityTopic, state, true, ct);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        if (!_board.IsConnected)
            return;

        var now = DateTime.UtcNow;
        foreach (var entity in _entities)
        {
            if (entity.PollInterval is not { } interval || _nextPoll.GetValueOrDefault(entity) > now)
                continue;

            _nextPoll[entity] = now + interval;
            await SafeAsync(entity, () => entity.PollAsync(_context, _board, this, now, _options.Heartbeat, ct));
        }
    }

    private async Task SafeAsync(HomeAssistantEntity entity, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failing sensor must not stop the others; the board service reports link problems through its own state.
            _logger.LogDebug("Entity '{Entity}' failed: {Message}", entity.Id, ex.Message);
        }
    }
}
